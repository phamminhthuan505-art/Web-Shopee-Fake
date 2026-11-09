using System;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109000000_InitialCreate")]
partial class InitialCreate
{
    /// <inheritdoc />
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("Ecommerce.Domain.Entities.Category", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Description")
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");

            b.Property<bool>("IsActive")
                .HasColumnType("bit");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique();

            b.ToTable("Categories", (string)null);
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Order", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<DateTime>("OrderDate")
                .HasColumnType("datetime2");

            b.Property<string>("OrderNumber")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<string>("PaymentMethod")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<string>("PaymentStatus")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<string>("ShippingAddress")
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("nvarchar(255)");

            b.Property<int>("Status")
                .HasColumnType("int");

            b.Property<decimal>("TotalAmount")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.Property<int>("UserId")
                .HasColumnType("int");

            b.HasKey("Id");

            b.HasIndex("OrderNumber")
                .IsUnique();

            b.HasIndex("UserId");

            b.ToTable("Orders", (string)null);
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.OrderDetail", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<int>("OrderId")
                .HasColumnType("int");

            b.Property<int>("ProductId")
                .HasColumnType("int");

            b.Property<int>("Quantity")
                .HasColumnType("int");

            b.Property<decimal>("SubTotal")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.Property<decimal>("UnitPrice")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.HasKey("Id");

            b.HasIndex("OrderId");

            b.HasIndex("ProductId");

            b.ToTable("OrderDetails", (string)null);
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Product", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<int>("CategoryId")
                .HasColumnType("int");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Description")
                .HasColumnType("nvarchar(max)");

            b.Property<string>("ImageUrl")
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");

            b.Property<bool>("IsActive")
                .HasColumnType("bit");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            b.Property<decimal>("Price")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.Property<string>("Sku")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<int>("StockQuantity")
                .HasColumnType("int");

            b.HasKey("Id");

            b.HasIndex("CategoryId");

            b.HasIndex("Sku")
                .IsUnique();

            b.ToTable("Products", (string)null);
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Review", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<string>("Comment")
                .HasMaxLength(1000)
                .HasColumnType("nvarchar(1000)");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<int>("ProductId")
                .HasColumnType("int");

            b.Property<int>("Rating")
                .HasColumnType("int");

            b.Property<int>("UserId")
                .HasColumnType("int");

            b.HasKey("Id");

            b.HasIndex("ProductId");

            b.HasIndex("UserId");

            b.ToTable("Reviews", (string)null);
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Role", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Description")
                .HasMaxLength(255)
                .HasColumnType("nvarchar(255)");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique();

            b.ToTable("Roles", (string)null);
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.User", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("nvarchar(150)");

            b.Property<string>("FullName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<bool>("IsActive")
                .HasColumnType("bit");

            b.Property<string>("PasswordHash")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");

            b.Property<string>("PhoneNumber")
                .HasMaxLength(20)
                .HasColumnType("nvarchar(20)");

            b.Property<int>("RoleId")
                .HasColumnType("int");

            b.Property<string>("Username")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.HasKey("Id");

            b.HasIndex("Email")
                .IsUnique();

            b.HasIndex("RoleId");

            b.HasIndex("Username")
                .IsUnique();

            b.ToTable("Users", (string)null);
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Order", b =>
        {
            b.HasOne("Ecommerce.Domain.Entities.User", "User")
                .WithMany("Orders")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("User");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.OrderDetail", b =>
        {
            b.HasOne("Ecommerce.Domain.Entities.Order", "Order")
                .WithMany("OrderDetails")
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Ecommerce.Domain.Entities.Product", "Product")
                .WithMany("OrderDetails")
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Order");

            b.Navigation("Product");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Product", b =>
        {
            b.HasOne("Ecommerce.Domain.Entities.Category", "Category")
                .WithMany("Products")
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Category");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Review", b =>
        {
            b.HasOne("Ecommerce.Domain.Entities.Product", "Product")
                .WithMany("Reviews")
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Ecommerce.Domain.Entities.User", "User")
                .WithMany("Reviews")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Product");

            b.Navigation("User");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.User", b =>
        {
            b.HasOne("Ecommerce.Domain.Entities.Role", "Role")
                .WithMany("Users")
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Role");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Category", b =>
        {
            b.Navigation("Products");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Order", b =>
        {
            b.Navigation("OrderDetails");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Product", b =>
        {
            b.Navigation("OrderDetails");

            b.Navigation("Reviews");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.Role", b =>
        {
            b.Navigation("Users");
        });

        modelBuilder.Entity("Ecommerce.Domain.Entities.User", b =>
        {
            b.Navigation("Orders");

            b.Navigation("Reviews");
        });
#pragma warning restore 612, 618
    }
}
