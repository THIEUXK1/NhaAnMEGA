using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnMEGA.Models.Zktime;

[Table("DEPARTMENTS")]
[Index("Deptname", Name = "DEPTNAME")]
public partial class Department
{
    [Key]
    [Column("DEPTID")]
    public int Deptid { get; set; }

    [Column("DEPTNAME")]
    [StringLength(30)]
    [Unicode(false)]
    public string? Deptname { get; set; }

    [Column("SUPDEPTID")]
    public int Supdeptid { get; set; }

    public short? InheritParentSch { get; set; }

    public short? InheritDeptSch { get; set; }

    public short? InheritDeptSchClass { get; set; }

    public short? AutoSchPlan { get; set; }

    public short? InLate { get; set; }

    public short? OutEarly { get; set; }

    public short? InheritDeptRule { get; set; }

    public int? MinAutoSchInterval { get; set; }

    [Column("RegisterOT")]
    public short? RegisterOt { get; set; }

    public int DefaultSchId { get; set; }

    [Column("ATT")]
    public short? Att { get; set; }

    public short? Holiday { get; set; }

    public short? OverTime { get; set; }

    [StringLength(255)]
    public string? TenTiengViet { get; set; }
}
