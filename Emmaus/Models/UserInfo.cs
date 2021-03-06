﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Emmaus.Models
{
    public class UserInfo
    {
        [Required, DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        public List<string> Roles { get; set; }
    }
}