﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHabitTracker.EntityFrameworkCore;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.1");

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.CategoryEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.ContentEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("CategoryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(13)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Contents");

                    b.HasDiscriminator().HasValue("ContentEntity");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.ItemEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DoneAt")
                        .HasColumnType("TEXT");

                    b.Property<long>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.PriorityEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Priorities");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.SettingsEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BaseUrl")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Culture")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("DisplayNoteContentAsMarkdown")
                        .HasColumnType("INTEGER");

                    b.Property<int>("FirstDayOfWeek")
                        .HasColumnType("INTEGER");

                    b.PrimitiveCollection<string>("HiddenCategoryIds")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("HorizontalMargin")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("InsertTabsInNoteContent")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDarkMode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("RememberMe")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SelectedRatio")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SelectedRatioMin")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowColor")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowCreatedUpdated")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowHelp")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowItemList")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowLargeCalendar")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowOnlyOverSelectedRatioMin")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ShowPriority")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("ShowPriority");

                    b.Property<bool>("ShowSmallCalendar")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SortBy")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("SortBy");

                    b.Property<string>("StartPage")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StartSidebar")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Theme")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("UncheckAllItemsOnHabitDone")
                        .HasColumnType("INTEGER");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("VerticalMargin")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.TimeEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("TEXT");

                    b.Property<long>("HabitId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("HabitId");

                    b.ToTable("Times");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.UserEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.HabitEntity", b =>
                {
                    b.HasBaseType("OpenHabitTracker.Data.Entities.ContentEntity");

                    b.Property<TimeOnly?>("Duration")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastTimeDoneAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("RepeatCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RepeatInterval")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RepeatPeriod")
                        .HasColumnType("INTEGER");

                    b.HasDiscriminator().HasValue("HabitEntity");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.NoteEntity", b =>
                {
                    b.HasBaseType("OpenHabitTracker.Data.Entities.ContentEntity");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasDiscriminator().HasValue("NoteEntity");
                });

            modelBuilder.Entity("OpenHabitTracker.Data.Entities.TaskEntity", b =>
                {
                    b.HasBaseType("OpenHabitTracker.Data.Entities.ContentEntity");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("TEXT");

                    b.Property<TimeOnly?>("Duration")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("PlannedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("StartedAt")
                        .HasColumnType("TEXT");

                    b.ToTable("Contents", t =>
                        {
                            t.Property("Duration")
                                .HasColumnName("TaskEntity_Duration");
                        });

                    b.HasDiscriminator().HasValue("TaskEntity");
                });
#pragma warning restore 612, 618
        }
    }
}
