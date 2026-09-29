using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnMEGA.Models.Zktime;

[Table("BlacklistPF")]
[Index("WorkerId", Name = "IX_BlacklistPfs_WorkerId")]
public partial class BlacklistPf
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("WorkerID")]
    [StringLength(50)]
    public string? WorkerId { get; set; }

    [StringLength(100)]
    public string? WorkerName { get; set; }
}
