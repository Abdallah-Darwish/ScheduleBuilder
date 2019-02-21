using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleBuilder.Core
{
    public enum UClassRegisterationNotificationType
    {
        Processing, Succeeded, SucceededAfterConfirmation, Failed, RequiredConfirmation, ClassIsFullOrDoesntExist
    }
    public struct UClassRegisterationNotification
    {
        public UClassRegisterationNotification(UClassRegisterationNotificationType type, UClass cls)
        {
            Class = cls;
            Type = type;
        }
        public UClassRegisterationNotificationType Type { get; }
        public UClass Class { get; }
    }
}
