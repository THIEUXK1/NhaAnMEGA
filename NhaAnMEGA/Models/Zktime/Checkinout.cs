using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnMEGA.Models.Zktime;

[PrimaryKey("Userid", "Checktime")]
[Table("CHECKINOUT")]
public partial class Checkinout
{
    [Key]
    [Column("USERID")]
    public int Userid { get; set; }

    [Key]
    [Column("CHECKTIME", TypeName = "timestamp without time zone")]
    public DateTime Checktime { get; set; }

    [Column("CHECKTYPE")]
    [StringLength(1)]
    [Unicode(false)]
    public string? Checktype { get; set; }

    [Column("VERIFYCODE")]
    public int? Verifycode { get; set; }

    [Column("SENSORID")]
    [StringLength(5)]
    [Unicode(false)]
    public string? Sensorid { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string? Memoinfo { get; set; }

    [StringLength(24)]
    [Unicode(false)]
    public string? WorkCode { get; set; }

    [Column("sn")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Sn { get; set; }

    public short? UserExtFmt { get; set; }
}
