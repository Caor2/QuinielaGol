using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Quiniela.Models
{
    public class predictionModels
    {
        public List<usrTranslations> userList { get; set; }
        public List<Prediction> userPredicts { get; set; }
        public List<Prediction> anotherPredicts { get; set; }

    }

    public class usrTranslations {
        public string Identifier { get; set; }
        public string Pseudo { get; set; }

    }
}