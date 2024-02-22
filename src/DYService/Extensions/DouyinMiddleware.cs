using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService.Extensions
{
    public class DouyinMiddleware
    {
        private readonly RequestDelegate _next;

        public DouyinMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            await _next(context);
        }
    }
}
