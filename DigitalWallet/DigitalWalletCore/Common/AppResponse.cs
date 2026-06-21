using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DigitalWalletCore.Common
{
    public class AppResponse<T>
    {
        public AppResponse()
        {
        }
        public AppResponse(T data, string message = null, string auditValues = "")
        {
            Succeeded = true;
            Message = message;
            Data = data;
            AuditValues = auditValues;
        }
        public AppResponse(string message)
        {
            Succeeded = false;
            Message = message;
        }
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public T Data { get; set; }
        [JsonIgnore]
        public string AuditValues { get; set; }
    }
}
