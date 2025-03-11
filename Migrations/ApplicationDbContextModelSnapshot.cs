﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VisitorLog_PBFD.Data;

#nullable disable

namespace VisitorLog_PBFD.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("VisitorLog_PBFD.Models.Location", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<int>("ChildId")
                        .HasColumnType("int");

                    b.Property<int>("Level")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("NameTypeId")
                        .HasColumnType("int");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Locations");
                });

            modelBuilder.Entity("VisitorLog_PBFD.Models.NameType", b =>
                {
                    b.Property<int>("NameTypeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("NameTypeId"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("NameTypeId");

                    b.ToTable("NameTypes");

                    b.HasData(
                        new
                        {
                            NameTypeId = 1,
                            Name = "Continent"
                        },
                        new
                        {
                            NameTypeId = 2,
                            Name = "Country"
                        },
                        new
                        {
                            NameTypeId = 3,
                            Name = "State"
                        },
                        new
                        {
                            NameTypeId = 4,
                            Name = "County"
                        },
                        new
                        {
                            NameTypeId = 5,
                            Name = "City"
                        },
                        new
                        {
                            NameTypeId = 6,
                            Name = "District"
                        },
                        new
                        {
                            NameTypeId = 7,
                            Name = "Province"
                        },
                        new
                        {
                            NameTypeId = 8,
                            Name = "Station"
                        },
                        new
                        {
                            NameTypeId = 9,
                            Name = "Special Administrative Region"
                        },
                        new
                        {
                            NameTypeId = 10,
                            Name = "Separate Political Entity"
                        },
                        new
                        {
                            NameTypeId = 11,
                            Name = "Region"
                        });
                });

            modelBuilder.Entity("VisitorLog_PBFD.Models.Person", b =>
                {
                    b.Property<int>("PersonId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("PersonId"));

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MiddleName")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("PersonId");

                    b.ToTable("Persons");
                });

            modelBuilder.Entity("VisitorLog_PBFD.Models.Report", b =>
                {
                    b.Property<string>("CityName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ContinentName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CountryName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CountyName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PersonName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StateName")
                        .HasColumnType("nvarchar(max)");

                    b.ToTable("Reports");
                });

            modelBuilder.Entity("VisitorLog_PBFD.Models.SchemaColumn", b =>
                {
                    b.Property<string>("COLUMN_NAME")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("COLUMN_NAME");

                    b.ToTable("SchemaColumns");
                });
#pragma warning restore 612, 618
        }
    }
}
