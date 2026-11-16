using System.Collections;
using DotNetNuke.Security.Roles;
using Vvf.Auth.Dipvvf.Components.Common;

namespace Vvf.Auth.Dipvvf.Components.Groups
{
    public class GroupInfo : RoleInfo, IAuthenticationObjectBase
    {
        private readonly ArrayList _authenticationMember = new ArrayList();

        public string Name
        {
            get
            {
                return RoleName;
            }
        }

        public ObjectClass ObjectClass
        {
            get
            {
                return ObjectClass.Group;
            }
        }

        public ArrayList AuthenticationMember
        {
            get
            {
                return _authenticationMember;
            }
        }

        public bool IsPopulated { get; set; }
    }
}