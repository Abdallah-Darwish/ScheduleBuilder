using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleBuilder.Core
{
    public enum UClassRegisterationNotificationType
    {
        Processing, Succeeded, SucceededAfterConfirmation, Failed, RequiredConfirmation, ClassIsFullOrDoesntExist
    }
    public readonly struct UClassRegisterationNotification
    {
        public UClassRegisterationNotification(UClassRegisterationNotificationType type, UClass cls)
        {
            Class = cls;
            Type = type;
        }
        public readonly UClassRegisterationNotificationType Type;
        public readonly UClass Class;
    }
}
