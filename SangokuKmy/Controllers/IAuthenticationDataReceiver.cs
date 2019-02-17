using System;
using SangokuKmy.Models.Data.Entities;
namespace SangokuKmy.Controllers
{
  public interface IAuthenticationDataReceiver
  {
    AuthenticationData AuthData { set; }
  }
}
