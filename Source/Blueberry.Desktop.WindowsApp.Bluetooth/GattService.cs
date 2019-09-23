
namespace Blueberry.Desktop.WindowsApp.Bluetooth
{
    /// <summary>
    /// details about a specific GATT Service
    /// <seealso cref="https://www.bluetooth.com/specifications/gatt/services/"/>
    /// </summary>
    public class GattService
    {

        #region Public Properties
        
        /// <summary>
        /// the human readable name for service
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// the uniform identifier that is unique to this service
        /// </summary>
        public string UniformTypeIdentifier { get; }

        /// <summary>
        /// the 16-bit assigned number for this service.
        /// the Bluetooth GATT Service UUID contains this
        /// </summary>
        public ushort AssignedNumber { get; }

        /// <summary>
        /// the type of specification that this service is
        /// <seealso cref="https://www.bluetooth.com/specifications/gatt/"/>
        /// </summary>
        public string ProfileSpecification { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// default constructor
        /// </summary>
        public GattService(string name, string uniformIdentifier, ushort assignedNumber, string profileSpecification)
        {
            Name = name;
            UniformTypeIdentifier = uniformIdentifier;
            AssignedNumber = assignedNumber;
            ProfileSpecification = profileSpecification;
        }
        #endregion

    }
}
