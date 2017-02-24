using Cirrious.MvvmCross.Plugins.Messenger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Messages
{

    public class GatewayInstructionNotificationMessage
        : MvxMessage
    {

        public enum NotificationCommand { Add, Update, Delete };

        private IDictionary<Guid, NotificationCommand> _instructionNotifications = null;

        public GatewayInstructionNotificationMessage(object sender, IDictionary<Guid, NotificationCommand> instructionNotifications)
            : base(sender)
        {
            _instructionNotifications = instructionNotifications;
        }

        public GatewayInstructionNotificationMessage(object sender, Guid instructionID, NotificationCommand notificationCommand)
            : this(sender, new Dictionary<Guid, NotificationCommand> { { instructionID, notificationCommand } })
        { }

        public IDictionary<Guid, NotificationCommand> InstructionNotifications
        {
            get { return _instructionNotifications; }
        }

        private IEnumerable<Guid> _insertedInstructionIDs = null;
        public IEnumerable<Guid> InsertedInstructionIDs
        {
            get { return _insertedInstructionIDs ?? (_insertedInstructionIDs = this.GetInstructionIDs(NotificationCommand.Add)); }
        }

        private IEnumerable<Guid> _updatedInstructionIDs = null;
        public IEnumerable<Guid> UpdatedInstructionIDs
        {
            get { return _updatedInstructionIDs ?? (_updatedInstructionIDs = this.GetInstructionIDs(NotificationCommand.Update)); }
        }

        private IEnumerable<Guid> _deletedInstructionIDs = null;
        public IEnumerable<Guid> DeletedInstructionIDs
        {
            get { return _deletedInstructionIDs ?? (_deletedInstructionIDs = this.GetInstructionIDs(NotificationCommand.Delete)); }
        }

        private IEnumerable<Guid> GetInstructionIDs(NotificationCommand notificationCommand)
        {
            return _instructionNotifications.Where(kvp => kvp.Value == notificationCommand).Select(kvp => kvp.Key);
        }

    }

}
