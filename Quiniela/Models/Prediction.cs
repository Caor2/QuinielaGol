//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Quiniela.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Prediction
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int MatchId { get; set; }
        public int LocalGoals { get; set; }
        public int VisitorGoals { get; set; }
    
        public virtual Match Match { get; set; }
    }
}
