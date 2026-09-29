using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnMEGA.Models.Zktime;

[Table("USERINFO")]
[Index("Badgenumber", Name = "BADGENUMBER", IsUnique = true)]
[Index("Ssn", Name = "IX_Userinfos_Ssn")]
public partial class Userinfo
{
    [Key]
    [Column("USERID")]
    public int Userid { get; set; }

    [Column("BADGENUMBER")]
    [StringLength(24)]
    [Unicode(false)]
    public string Badgenumber { get; set; } = null!;

    [Column("SSN")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Ssn { get; set; }

    [Column("NAME")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Column("GENDER")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Gender { get; set; }

    [Column("TITLE")]
    [StringLength(100)]
    public string? Title { get; set; }

    [Column("PAGER")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Pager { get; set; }

    [Column("BIRTHDAY", TypeName = "timestamp without time zone")]
    public DateTime? Birthday { get; set; }

    [Column("HIREDDAY", TypeName = "timestamp without time zone")]
    public DateTime? Hiredday { get; set; }

    [Column("STREET")]
    [StringLength(80)]
    [Unicode(false)]
    public string? Street { get; set; }

    [Column("CITY")]
    [StringLength(2)]
    [Unicode(false)]
    public string? City { get; set; }

    [Column("STATE")]
    [StringLength(2)]
    [Unicode(false)]
    public string? State { get; set; }

    [Column("ZIP")]
    [StringLength(12)]
    [Unicode(false)]
    public string? Zip { get; set; }

    [Column("OPHONE")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Ophone { get; set; }

    [Column("FPHONE")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Fphone { get; set; }

    [Column("VERIFICATIONMETHOD")]
    public short? Verificationmethod { get; set; }

    [Column("DEFAULTDEPTID")]
    public short? Defaultdeptid { get; set; }

    [Column("SECURITYFLAGS")]
    public short? Securityflags { get; set; }

    [Column("ATT")]
    public short Att { get; set; }

    [Column("INLATE")]
    public short Inlate { get; set; }

    [Column("OUTEARLY")]
    public short Outearly { get; set; }

    [Column("OVERTIME")]
    public short Overtime { get; set; }

    [Column("SEP")]
    public short Sep { get; set; }

    [Column("HOLIDAY")]
    public short Holiday { get; set; }

    [Column("MINZU")]
    [StringLength(50)]
    public string? Minzu { get; set; }

    [Column("PASSWORD")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Password { get; set; }

    [Column("LUNCHDURATION")]
    public short Lunchduration { get; set; }

    [Column("MVerifyPass")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MverifyPass { get; set; }

    [Column("PHOTO", TypeName = "bytea")]
    public byte[]? Photo { get; set; }

    [Column(TypeName = "bytea")]
    public byte[]? Notes { get; set; }

    [Column("privilege")]
    public int? Privilege { get; set; }

    public short? InheritDeptSch { get; set; }

    public short? InheritDeptSchClass { get; set; }

    public short? AutoSchPlan { get; set; }

    public int? MinAutoSchInterval { get; set; }

    [Column("RegisterOT")]
    public short? RegisterOt { get; set; }

    public short? InheritDeptRule { get; set; }

    [Column("EMPRIVILEGE")]
    public short? Emprivilege { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? CardNo { get; set; }

    public int? FaceGroup { get; set; }

    public int? AccGroup { get; set; }

    [Column("UseAccGroupTZ")]
    public int? UseAccGroupTz { get; set; }

    public int? VerifyCode { get; set; }

    public int? Expires { get; set; }

    public int? ValidCount { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? ValidTimeBegin { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? ValidTimeEnd { get; set; }

    public int? TimeZone1 { get; set; }

    public int? TimeZone2 { get; set; }

    public int? TimeZone3 { get; set; }

    [Column("DEFAULTDEPTID2")]
    [StringLength(255)]
    public string? Defaultdeptid2 { get; set; }

    [StringLength(255)]
    public string? TenTrungQuoc { get; set; }

    [StringLength(255)]
    public string? BoPhan { get; set; }
}
