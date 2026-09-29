using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnMEGA.Models.Zktime;

[Table("LuuQuetPF")]
public partial class LuuQuetPf
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string? CardCode { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? LogTime { get; set; }

    [StringLength(255)]
    public string? LyDo { get; set; }
}
