using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Warsztat.Models;

public partial class WarsztatdbContext : DbContext
{
    public WarsztatdbContext()
    {
    }

    public WarsztatdbContext(DbContextOptions<WarsztatdbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Handoverprotocol> Handoverprotocols { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Part> Parts { get; set; }

    public virtual DbSet<Service> Services { get; set; }

   
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb3_general_ci")
            .HasCharSet("utf8mb3");

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("car");

            entity.HasIndex(e => e.Id, "ID_UNIQUE").IsUnique();

            entity.HasIndex(e => e.RegistrationNumber, "RegistrationNumber_UNIQUE").IsUnique();

            entity.HasIndex(e => e.ClientId, "fk_ClientID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(10);
            entity.Property(e => e.Vin)
                .HasMaxLength(17)
                .HasColumnName("VIN");

            entity.HasOne(d => d.Client).WithMany(p => p.Cars)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ClientID");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("client");

            entity.HasIndex(e => e.Email, "Email_UNIQUE").IsUnique();

            entity.HasIndex(e => e.Id, "ID_UNIQUE").IsUnique();

            entity.HasIndex(e => e.Password, "Password_UNIQUE").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "PhoneNumber_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Address).HasMaxLength(120);
            entity.Property(e => e.Email).HasMaxLength(90);
            entity.Property(e => e.FirstName).HasMaxLength(40);
            entity.Property(e => e.LastName).HasMaxLength(60);
            entity.Property(e => e.Password).HasMaxLength(60);
            entity.Property(e => e.PhoneNumber).HasMaxLength(9);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("employee");

            entity.HasIndex(e => e.Email, "Email_UNIQUE").IsUnique();

            entity.HasIndex(e => e.Id, "ID_UNIQUE").IsUnique();

            entity.HasIndex(e => e.Password, "Password_UNIQUE").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "PhoneNumber_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Email).HasMaxLength(90);
            entity.Property(e => e.FirstName).HasMaxLength(40);
            entity.Property(e => e.LastName).HasMaxLength(60);
            entity.Property(e => e.Password).HasMaxLength(60);
            entity.Property(e => e.PhoneNumber).HasMaxLength(9);
        });

        modelBuilder.Entity<Handoverprotocol>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PRIMARY");

            entity.ToTable("handoverprotocol");

            entity.HasIndex(e => e.OrderId, "ID_UNIQUE").IsUnique();

            entity.HasIndex(e => e.PictureLink, "PictureLink_UNIQUE").IsUnique();

            entity.Property(e => e.OrderId)
                .ValueGeneratedNever()
                .HasColumnName("OrderID");
            entity.Property(e => e.PictureLink).HasMaxLength(70);
            entity.Property(e => e.ProtocolLink).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithOne(p => p.Handoverprotocol)
                .HasForeignKey<Handoverprotocol>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fkHP_OrderID");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("order");

            entity.HasIndex(e => e.ClientId, "ClientID_idx");

            entity.HasIndex(e => e.EmployeeId, "EmployeeNumber_idx");

            entity.HasIndex(e => e.Id, "ID_UNIQUE").IsUnique();

            entity.HasIndex(e => e.CarId, "fkOrder_CarID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CarId).HasColumnName("CarID");
            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.InvoiceLink).HasMaxLength(45);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(45);

            entity.HasOne(d => d.Car).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fkOrder_CarID");

            entity.HasOne(d => d.Client).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fkOrder_ClientID");

            entity.HasOne(d => d.Employee).WithMany(p => p.Orders)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("fk_EmployeeID");

            entity.HasMany(d => d.Services).WithMany(p => p.Orders)
                .UsingEntity<Dictionary<string, object>>(
                    "Orderdetail",
                    r => r.HasOne<Service>().WithMany()
                        .HasForeignKey("ServiceId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fkOD_ServiceID"),
                    l => l.HasOne<Order>().WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fkOD_OrderID"),
                    j =>
                    {
                        j.HasKey("OrderId", "ServiceId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("orderdetail");
                        j.HasIndex(new[] { "ServiceId" }, "ServiceID_idx");
                        j.IndexerProperty<int>("OrderId").HasColumnName("OrderID");
                        j.IndexerProperty<int>("ServiceId").HasColumnName("ServiceID");
                    });
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("part");

            entity.HasIndex(e => e.Id, "ID_UNIQUE").IsUnique();

            entity.HasIndex(e => e.OrderId, "fkPart_OrderID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(90);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Price).HasPrecision(6, 2);
            entity.Property(e => e.SerialNumber).HasMaxLength(45);

            entity.HasOne(d => d.Order).WithMany(p => p.Parts)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fkPart_OrderID");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("service");

            entity.HasIndex(e => e.Id, "ID_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(120);
            entity.Property(e => e.Price).HasPrecision(6, 2);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
