using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Quiniela.Models
{
    public class PredictionModels
    {
        public bool allowUpload { get; set; }
        public List<Prediction> userPredicts { get; set; }
    }
}