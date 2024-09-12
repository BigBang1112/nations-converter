﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NationsConverterWeb;

#nullable disable

namespace NationsConverterWeb.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("NationsConverterWeb.Models.Block", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset?>("AssignedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("AssignedToId")
                        .HasColumnType("int");

                    b.Property<string>("CategoryId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasMaxLength(32767)
                        .HasColumnType("longtext");

                    b.Property<string>("EnvironmentId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("HasUpload")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("IconWebp")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsDone")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("PageName")
                        .IsRequired()
                        .HasMaxLength(32767)
                        .HasColumnType("longtext");

                    b.Property<string>("SubCategoryId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("AssignedToId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("EnvironmentId");

                    b.HasIndex("SubCategoryId");

                    b.ToTable("Blocks");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.BlockItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("BlockId")
                        .HasColumnType("int");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("JustResave")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Modifier")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<int>("SubVariant")
                        .HasColumnType("int");

                    b.Property<int>("Value")
                        .HasColumnType("int");

                    b.Property<int>("Variant")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BlockId");

                    b.ToTable("BlockItems");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.ConverterCategory", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ConverterCategories");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.ConverterSubCategory", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ConverterSubCategories");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.DiscordUser", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<ulong>("Id"));

                    b.Property<string>("AvatarHash")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("ConnectedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("GlobalName")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("DiscordUsers");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.GameEnvironment", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("GameEnvironments");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.ItemUpload", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("BlockItemId")
                        .HasColumnType("int");

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTimeOffset>("LastModifiedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OriginalFileName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("UploadedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("UploadedById")
                        .HasColumnType("int");

                    b.Property<int>("Value")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BlockItemId");

                    b.HasIndex("UploadedById");

                    b.ToTable("ItemUploads");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsDeveloper")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsModeler")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTimeOffset>("JoinedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.Block", b =>
                {
                    b.HasOne("NationsConverterWeb.Models.User", "AssignedTo")
                        .WithMany()
                        .HasForeignKey("AssignedToId");

                    b.HasOne("NationsConverterWeb.Models.ConverterCategory", "Category")
                        .WithMany("Blocks")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("NationsConverterWeb.Models.GameEnvironment", "Environment")
                        .WithMany("Blocks")
                        .HasForeignKey("EnvironmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("NationsConverterWeb.Models.ConverterSubCategory", "SubCategory")
                        .WithMany("Blocks")
                        .HasForeignKey("SubCategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AssignedTo");

                    b.Navigation("Category");

                    b.Navigation("Environment");

                    b.Navigation("SubCategory");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.BlockItem", b =>
                {
                    b.HasOne("NationsConverterWeb.Models.Block", "Block")
                        .WithMany("Items")
                        .HasForeignKey("BlockId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Block");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.DiscordUser", b =>
                {
                    b.HasOne("NationsConverterWeb.Models.User", "User")
                        .WithOne("DiscordUser")
                        .HasForeignKey("NationsConverterWeb.Models.DiscordUser", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.ItemUpload", b =>
                {
                    b.HasOne("NationsConverterWeb.Models.BlockItem", "BlockItem")
                        .WithMany("Uploads")
                        .HasForeignKey("BlockItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("NationsConverterWeb.Models.User", "UploadedBy")
                        .WithMany()
                        .HasForeignKey("UploadedById");

                    b.Navigation("BlockItem");

                    b.Navigation("UploadedBy");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.Block", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.BlockItem", b =>
                {
                    b.Navigation("Uploads");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.ConverterCategory", b =>
                {
                    b.Navigation("Blocks");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.ConverterSubCategory", b =>
                {
                    b.Navigation("Blocks");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.GameEnvironment", b =>
                {
                    b.Navigation("Blocks");
                });

            modelBuilder.Entity("NationsConverterWeb.Models.User", b =>
                {
                    b.Navigation("DiscordUser");
                });
#pragma warning restore 612, 618
        }
    }
}
