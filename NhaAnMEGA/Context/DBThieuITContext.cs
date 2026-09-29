using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NhaAnMEGA.Models.NhaAnMEGA;
using NhaAnMEGA.Utils;

namespace NhaAnMEGA.Context;

public partial class DBThieuITContext : DbContext
{
    public DBThieuITContext()
    {
    }

    public DBThieuITContext(DbContextOptions<DBThieuITContext> options)
        : base(options)
    {
    }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(ChuoiKetNoi.GiaTri);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NhanVien__3214EC276DC13B50");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
