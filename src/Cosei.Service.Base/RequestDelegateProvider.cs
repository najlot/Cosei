using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace Cosei.Service.Base
{
	internal class RequestDelegateProvider : IRequestDelegateProvider
	{
		private IApplicationBuilder _applicationBuilder;
		private RequestDelegate _requestDelegate = null;

		public RequestDelegate RequestDelegate
		{
			get
			{
				if (_requestDelegate == null)
				{
					_requestDelegate = _applicationBuilder.Build();
				}

				return _requestDelegate;
			}
		}

		public void SetApplicationBuilder(IApplicationBuilder applicationBuilder)
		{
			_applicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
		}
	}
}