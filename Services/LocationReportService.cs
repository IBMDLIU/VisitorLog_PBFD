using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using VisitorLog_PBFD.Data;

namespace VisitorLog_PBFD.Services
{
    public class LocationReportService : ILocationReportService
    {
        private readonly ApplicationDbContext _context;
        private int _personId;
        private Dictionary<string, List<string>> _columnCache = new();
        private Dictionary<string, List<ChildViewModel>> _childrenCache = new();
        private Dictionary<string, Dictionary<string, object>> _columnValuesCache = new();

        // Constructor: Initializes the processor with database context and person ID
        public LocationReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Entry point: Generates all paths asynchronously
        public async Task<HashSet<string>> GetPathsAsync(int personId)
        {
            _personId = personId;
            await InitializeCachesAsync(); // Prepare caches for efficient processing
            var paths = new HashSet<string>();
            var processingQueue = new Queue<ProcessingItem>();

            // Initialize the queue with root items
            processingQueue.Enqueue(new ProcessingItem("ContinentRoots", ""));
            processingQueue.Enqueue(new ProcessingItem("Continent", ""));

            // Process items iteratively
            while (processingQueue.Count > 0)
            {
                var currentItem = processingQueue.Dequeue();
                ProcessItemAsync(currentItem, processingQueue, paths);
            }

            return paths;
        }

        // Initializes caches for columns, children, and column values
        private async Task InitializeCachesAsync()
        {
            await using var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            await CacheColumnMetadataAsync((SqlConnection)connection);
            await CacheChildrenAsync((SqlConnection)connection); // Hierarchy implementation
            await CacheColumnValuesAsync((SqlConnection)connection);
        }

        // Caches metadata for columns present in tables with a 'PersonId' column
        private async Task CacheColumnMetadataAsync(SqlConnection connection)
        {
            var query = @"
                SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME IN (
                    SELECT DISTINCT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE COLUMN_NAME = 'PersonId'
                ) AND COLUMN_NAME NOT IN ('PersonId', 'IsDeleted')";

            await using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0);
                var column = reader.GetString(1);

                // Cache column names for each table
                if (!_columnCache.ContainsKey(table))
                    _columnCache[table] = new List<string>();

                _columnCache[table].Add(column);
            }
        }

        // Caches children nodes using a recursive CTE to build a hierarchy
        private async Task CacheChildrenAsync(SqlConnection connection)
        {
            var query = @"
                WITH RecursiveCTE AS (
                    SELECT 
                        Id, 
                        Name, 
                        ParentId,
                        ChildId,
                        CAST(Name AS NVARCHAR(MAX)) AS HierarchyPath,
                        CAST(NULL AS NVARCHAR(MAX)) AS ParentName,
                        0 AS Level
                    FROM Locations
                    WHERE ParentId IS NULL

                    UNION ALL

                    SELECT 
                        l.Id, 
                        l.Name, 
                        l.ParentId,
                        l.ChildId,
                        CAST(r.HierarchyPath + ' > ' + l.Name AS NVARCHAR(MAX)),
                        r.Name AS ParentName,
                        r.Level + 1
                    FROM Locations l
                    INNER JOIN RecursiveCTE r ON l.ParentId = r.Id
                )
                SELECT 
                    COALESCE(ParentName, 'ContinentRoots') AS ParentKey,
                    Id,
                    ChildId,
                    Name,
                    ParentName,
                    HierarchyPath
                FROM RecursiveCTE
                ORDER BY Level, HierarchyPath";

            await using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var parentKey = reader.GetString(0);
                var child = new ChildViewModel(
                    Id: reader.GetInt32(1),
                    ChildId: reader.GetInt32(2),
                    Name: reader.GetString(3),
                    ParentName: reader.IsDBNull(4) ? "ContinentRoots" : reader.GetString(4),
                    HierarchyPath: reader.GetString(5)
                );

                // Cache children by their parent name
                if (!_childrenCache.ContainsKey(parentKey))
                    _childrenCache[parentKey] = new List<ChildViewModel>();

                _childrenCache[parentKey].Add(child);
            }
        }

        // Caches column values for each table
        private async Task CacheColumnValuesAsync(SqlConnection connection)
        {
            foreach (var table in _columnCache.Keys)
            {
                var columns = _columnCache[table];
                var query = $@"
                    SELECT {string.Join(", ", columns.Select(c => $"[{c}]"))}
                    FROM [{table}]
                    WHERE PersonId = {_personId}";

                await using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    _columnValuesCache[table] = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        _columnValuesCache[table][columnName] = reader.GetValue(i);
                    }
                }
            }
        }

        // Processes items from the queue, updating paths as needed
        private void ProcessItemAsync(ProcessingItem item, Queue<ProcessingItem> queue, HashSet<string> paths)
        {
            if (!_columnCache.TryGetValue(item.TableName, out var columns))
                return;

            foreach (var column in columns)
            {
                if (!_columnValuesCache.TryGetValue(item.TableName, out var columnValues) ||
                    !columnValues.TryGetValue(column, out var value))
                {
                    continue;
                }

                // Determine children that match the current column's value
                var children = GetChildrenForColumn(item.TableName, column, value);

                foreach (var child in children)
                {
                    // Add new paths and enqueue children for further processing
                    if (_childrenCache.ContainsKey(child.Name))
                    {
                        queue.Enqueue(new ProcessingItem(child.Name, child.HierarchyPath));
                    }
                    else
                    {
                        paths.Add(child.HierarchyPath);
                    }
                }
            }
        }

        // Finds children nodes based on a given column value
        private IEnumerable<ChildViewModel> GetChildrenForColumn(
            string tableName, string column, object value)
        {
            if (!_childrenCache.TryGetValue(column, out var allChildren))
                yield break;

            foreach (var child in allChildren)
            {
                bool isMatch = value switch
                {
                    string strValue when BigInteger.TryParse(strValue, out var bigInt)
                        => (bigInt & (BigInteger.One << child.ChildId)) != 0,
                    int intValue
                        => (intValue & (1 << child.ChildId)) != 0,
                    long longValue
                        => (longValue & (1L << child.ChildId)) != 0,
                    _ => false
                };

                if (isMatch)
                    yield return child;
            }
        }

        // ViewModel representing a location child
        private record ChildViewModel(
            int Id,
            int ChildId,
            string Name,
            string ParentName,
            string HierarchyPath
        );

        // Record for tracking items during processing
        private record ProcessingItem(string TableName, string CurrentPath);
    }
}