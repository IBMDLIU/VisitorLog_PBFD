using VisitorLog_PBFD.Data;
using VisitorLog_PBFD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorLog_PBFD.ViewModels;
using VisitorLog_PBFD.Services;

public class ContinentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILocationResetService _locationResetService;

    public ContinentsController(ApplicationDbContext context, ILocationResetService locationService)
    {
        _context = context;
        _locationResetService = locationService;
    }

    // GET: /Continents
    public async Task<IActionResult> Index(int personId)
    {
        //return RedirectToAction("Index", "Summary", new { personId = personId });

        ViewBag.PersonId = personId;

        // Get selected continents for the person
        var selectedContinentIds = await _context.ContinentRoots
            .Where(p => p.PersonId == personId && !p.IsDeleted)
            .Select(p => p.Continent)
            .ToListAsync();

        // Get all continents with their NameType and construct the ViewModel
        var continentViewModels = await (
            from c in _context.Locations
            join nt in _context.NameTypes
                on c.NameTypeId equals nt.NameTypeId into nameTypeGroup
            from nt in nameTypeGroup.DefaultIfEmpty() // Left outer join
            where c.ParentId == 0
            select new ContinentViewModel
            {
                ContinentId = c.ChildId,
                ContinentName = c.Name,
                PersonId = personId,
                LocationId = c.Id,
                NameTypeName = nt != null ? nt.Name : "Unknown NameType",
                IsSelected = selectedContinentIds != null && selectedContinentIds.Any()? (selectedContinentIds.FirstOrDefault() & (1<<c.ChildId)) != 0: false
            }
        ).ToListAsync();


        return View(continentViewModels);
    }


    [HttpPost]
    public async Task<IActionResult> Index(int personId, string[] selectedContinents)
    {
        if (selectedContinents == null || !selectedContinents.Any())
        {
            ModelState.AddModelError("", "No continents were selected.");
            return RedirectToAction("Index", "Continents", personId);
        }

        var selectedContinentData = selectedContinents
         .Select(x => x.Split('|')) // Use char delimiter
         .Select(parts => (ContinentId: int.Parse(parts[0]), LocationID: int.Parse(parts[1])))
         .ToList();

        await _locationResetService.ResetTableColumnsAsync("Continent", selectedContinentData, personId);

        // Calculate the bitmap value from the selectedContinentData array
        int bitmap = 0;
        foreach (var sc in selectedContinentData)
        {
            bitmap |= 1 << sc.ContinentId;
        }

        // Fetch the selected person's record from the database asynchronously
        var root = await _context.ContinentRoots
                                   .FirstOrDefaultAsync(p => p.PersonId == personId);

        //if a different selection is set
        if((root==null&&bitmap>0) || (root!=null&&root.Continent!=bitmap))
            await UpdateGrandparentTable(root, bitmap, personId); 

        // Extract LocationID list
        var locationIds = selectedContinentData
            .Select(data => data.LocationID)
            .ToList();

        // Serialize locationIds into a comma-separated string
        var locationIdsString = string.Join(",", locationIds);

        // Pass the serialized string in the RedirectToAction parameters
        return RedirectToAction("Index", "Locations", new { PersonId = personId, selectedLocationIds = locationIdsString });
    }

    private async Task UpdateGrandparentTable(ContinentRoot? root, int bitmap, int personId) {
        if (root != null)
        {
            // Update the continent column with the calculated bitmap
            root.Continent = bitmap;
            root.IsDeleted = false;
            // Save changes to the database asynchronously
            await _context.SaveChangesAsync();
        }
        else
        {
            // Insert a new record into _context.ContinentRoots
            var newRoot = new ContinentRoot
            {
                PersonId = personId,
                Continent = bitmap,
                IsDeleted = false
            };

            _context.ContinentRoots.Add(newRoot);
            // Save changes to the database asynchronously
            await _context.SaveChangesAsync();
        }
    }
}
