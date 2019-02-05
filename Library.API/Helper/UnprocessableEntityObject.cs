using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helper
{
    public class UnprocessableEntityObject : ObjectResult
    {
        public UnprocessableEntityObject(ModelStateDictionary modelsate):base(new SerializableError(modelsate))
        {
            if (modelsate == null)
                throw new Exception(nameof(modelsate));
            StatusCode = 422;
        }
    }
}
