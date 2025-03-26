using Microsoft.AspNetCore.Http;

namespace Cosei.Service.Base;

public interface IRequestDelegateProvider
{
	RequestDelegate RequestDelegate { get; }
}