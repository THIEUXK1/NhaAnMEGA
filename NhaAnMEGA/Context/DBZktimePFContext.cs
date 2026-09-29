using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NhaAnMEGA.Models.Zktime;
using NhaAnMEGA.Utils;

namespace NhaAnMEGA.Context;

public partial class DBZktimePFContext : DbContext
{
    public DBZktimePFContext()
    {
    }

    public DBZktimePFContext(DbContextOptions<DBZktimePFContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BlacklistPf> BlacklistPfs { get; set; }

    public virtual DbSet<Checkinout> Checkinouts { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<GatePf> GatePfs { get; set; }

    public virtual DbSet<KhachAnPf> KhachAnPfs { get; set; }

    public virtual DbSet<LuuQuetPf> LuuQuetPfs { get; set; }

    public virtual DbSet<TimeRecodePf> TimeRecodePfs { get; set; }

    public virtual DbSet<Userinfo> Userinfos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(ChuoiKetNoi.GiaTri);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlacklistPf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Blacklis__3213E83F1EEB424F");
        });

        modelBuilder.Entity<Checkinout>(entity =>
        {
            entity.HasKey(e => new { e.Userid, e.Checktime }).HasName("USERCHECKTIME");

            entity.Property(e => e.Checktime).HasDefaultValueSql("LOCALTIMESTAMP");
            entity.Property(e => e.Checktype).HasDefaultValue("I");
            entity.Property(e => e.UserExtFmt).HasDefaultValue((short)0);
            entity.Property(e => e.Verifycode).HasDefaultValue(0);
            entity.Property(e => e.WorkCode).HasDefaultValueSql("'0'");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Deptid).HasName("DEPTID");

            entity.Property(e => e.Att).HasDefaultValue((short)1);
            entity.Property(e => e.AutoSchPlan).HasDefaultValue((short)1);
            entity.Property(e => e.DefaultSchId).HasDefaultValue(1);
            entity.Property(e => e.Holiday).HasDefaultValue((short)1);
            entity.Property(e => e.InLate).HasDefaultValue((short)1);
            entity.Property(e => e.InheritDeptRule).HasDefaultValue((short)1);
            entity.Property(e => e.InheritDeptSch).HasDefaultValue((short)1);
            entity.Property(e => e.InheritDeptSchClass).HasDefaultValue((short)1);
            entity.Property(e => e.InheritParentSch).HasDefaultValue((short)1);
            entity.Property(e => e.MinAutoSchInterval).HasDefaultValue(24);
            entity.Property(e => e.OutEarly).HasDefaultValue((short)1);
            entity.Property(e => e.OverTime).HasDefaultValue((short)1);
            entity.Property(e => e.RegisterOt).HasDefaultValue((short)1);
            entity.Property(e => e.Supdeptid).HasDefaultValue(1);
        });

        modelBuilder.Entity<GatePf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Gate__3214EC279EB1AE61");
        });

        modelBuilder.Entity<KhachAnPf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__KhachAnP__3213E83FD0F0E7D0");
        });

        modelBuilder.Entity<LuuQuetPf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LuuQuet__3214EC07D5D87AF7");
        });

        modelBuilder.Entity<TimeRecodePf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Time_Rec__3213E83FE2E23B0D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("LOCALTIMESTAMP");
        });

        modelBuilder.Entity<Userinfo>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("USERIDS");

            entity.Property(e => e.AccGroup).HasDefaultValue(1);
            entity.Property(e => e.Att).HasDefaultValue((short)1);
            entity.Property(e => e.AutoSchPlan).HasDefaultValue((short)1);
            entity.Property(e => e.Defaultdeptid).HasDefaultValue((short)1);
            entity.Property(e => e.Emprivilege).HasDefaultValue((short)0);
            entity.Property(e => e.Expires).HasDefaultValue(0);
            entity.Property(e => e.FaceGroup).HasDefaultValue(0);
            entity.Property(e => e.Holiday).HasDefaultValue((short)1);
            entity.Property(e => e.InheritDeptRule).HasDefaultValue((short)1);
            entity.Property(e => e.InheritDeptSch).HasDefaultValue((short)1);
            entity.Property(e => e.InheritDeptSchClass).HasDefaultValue((short)1);
            entity.Property(e => e.Inlate).HasDefaultValue((short)1);
            entity.Property(e => e.Lunchduration).HasDefaultValue((short)1);
            entity.Property(e => e.MinAutoSchInterval).HasDefaultValue(24);
            entity.Property(e => e.Outearly).HasDefaultValue((short)1);
            entity.Property(e => e.Overtime).HasDefaultValue((short)1);
            entity.Property(e => e.Privilege).HasDefaultValue(0);
            entity.Property(e => e.RegisterOt).HasDefaultValue((short)1);
            entity.Property(e => e.Sep).HasDefaultValue((short)1);
            entity.Property(e => e.TimeZone1).HasDefaultValue(1);
            entity.Property(e => e.TimeZone2).HasDefaultValue(0);
            entity.Property(e => e.TimeZone3).HasDefaultValue(0);
            entity.Property(e => e.UseAccGroupTz).HasDefaultValue(1);
            entity.Property(e => e.ValidCount).HasDefaultValue(0);
            entity.Property(e => e.VerifyCode).HasDefaultValue(0);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
