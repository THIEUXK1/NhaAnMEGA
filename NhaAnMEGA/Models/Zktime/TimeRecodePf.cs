using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnMEGA.Models.Zktime;

[Table("Time_RecodePF")]
[Index("WorkerId", "DateTime", Name = "IX_TimeRecodePfs_WorkerId_DateTime", IsDescending = new[] { false, true })]
public partial class TimeRecodePf
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("WorkerID")]
    [StringLength(50)]
    public string? WorkerId { get; set; }

    [StringLength(100)]
    public string? WorkerName { get; set; }

    [Column("CardID")]
    [StringLength(50)]
    public string? CardId { get; set; }

    [Column("CardIC")]
    [StringLength(50)]
    public string? CardIc { get; set; }

    [Column("DepID")]
    [StringLength(50)]
    public string? DepId { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? Mealtime { get; set; }

    [Column("date", TypeName = "timestamp without time zone")]
    public DateTime? Date { get; set; }

    [Column("dateTime", TypeName = "timestamp without time zone")]
    public DateTime? DateTime { get; set; }

    [StringLength(50)]
    public string? So { get; set; }

    [Column("G_Name")]
    [StringLength(100)]
    public string? GName { get; set; }

    [StringLength(50)]
    public string? Bua { get; set; }

    public byte[]? ImageData { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [StringLength(500)]
    public string? ImagePath { get; set; }
}
