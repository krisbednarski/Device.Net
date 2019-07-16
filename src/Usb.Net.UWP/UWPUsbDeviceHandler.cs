﻿using Device.Net;
using Device.Net.Exceptions;
using Device.Net.UWP;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using windowsUsbDevice = Windows.Devices.Usb.UsbDevice;

namespace Usb.Net.UWP
{
    public class UWPUsbDeviceHandler : UWPDeviceHandlerBase<windowsUsbDevice>, IUsbDeviceHandler
    {
        #region Fields
        private bool disposed;
        #endregion

        #region Public Properties
        public UsbInterfaceHandler UsbInterfaceHandler { get; }
        #endregion

        #region Public Override Properties
        public override ushort WriteBufferSize => WriteUsbInterface.WriteEndpoint.WriteBufferSize;
        public override ushort ReadBufferSize => ReadUsbInterface.ReadEndpoint.ReadBufferSize;

        public IUsbInterface ReadUsbInterface
        {
            get => UsbInterfaceHandler.ReadUsbInterface;
            set => UsbInterfaceHandler.ReadUsbInterface = value;
        }

        public IUsbInterface WriteUsbInterface
        {
            get => UsbInterfaceHandler.WriteUsbInterface;
            set => UsbInterfaceHandler.WriteUsbInterface = value;
        }

        public IList<IUsbInterface> UsbInterfaces => UsbInterfaceHandler.UsbInterfaces;
        #endregion

        #region Constructors
        public UWPUsbDeviceHandler(ILogger logger, ITracer tracer) : this(null, logger, tracer)
        {
        }

        public UWPUsbDeviceHandler(ConnectedDeviceDefinition deviceDefinition) : this(deviceDefinition, null, null)
        {
        }

        public UWPUsbDeviceHandler(ConnectedDeviceDefinition connectedDeviceDefinition, ILogger logger, ITracer tracer) : base(connectedDeviceDefinition?.DeviceId, logger, tracer)
        {
            ConnectedDeviceDefinition = connectedDeviceDefinition ?? throw new ArgumentNullException(nameof(connectedDeviceDefinition));
            UsbInterfaceHandler = new UsbInterfaceHandler(logger, tracer);
        }
        #endregion

        #region Private Methods
        public override async Task InitializeAsync()
        {
            if (disposed) throw new ValidationException(Messages.DeviceDisposedErrorMessage);

            await GetDeviceAsync(DeviceId);

            if (ConnectedDevice != null)
            {
                if (ConnectedDevice.Configuration.UsbInterfaces == null || ConnectedDevice.Configuration.UsbInterfaces.Count == 0)
                {
                    ConnectedDevice.Dispose();
                    throw new DeviceException(Messages.ErrorMessageNoInterfaceFound);
                }

                var interfaceIndex = 0;
                foreach (var usbInterface in ConnectedDevice.Configuration.UsbInterfaces)
                {
                    var uwpUsbInterface = new UWPUsbInterface(usbInterface, Logger, Tracer);

                    UsbInterfaceHandler.UsbInterfaces.Add(uwpUsbInterface);

                    if (ReadUsbInterface == null && uwpUsbInterface.ReadEndpoint != null)
                    {
                        ReadUsbInterface = uwpUsbInterface;
                    }

                    if (WriteUsbInterface == null && uwpUsbInterface.WriteEndpoint != null)
                    {
                        WriteUsbInterface = uwpUsbInterface;
                    }

                    if (UsbInterfaceHandler.InterruptUsbInterface == null && uwpUsbInterface.WriteInterruptEndpoint != null)
                    {
                        UsbInterfaceHandler.InterruptUsbInterface = uwpUsbInterface;
                    }

                    interfaceIndex++;
                }

                if (UsbInterfaceHandler.ReadUsbInterface == null && UsbInterfaceHandler.InterruptUsbInterface?.ReadInterruptEndpoint != null)
                {
                    Logger?.Log(Messages.WarningNoReadInterfaceFound, nameof(UWPUsbDeviceHandler), null, LogLevel.Warning);
                    UsbInterfaceHandler.ReadUsbInterface = UsbInterfaceHandler.InterruptUsbInterface;
                    UsbInterfaceHandler.ReadUsbInterface.ReadEndpoint = UsbInterfaceHandler.InterruptUsbInterface?.ReadInterruptEndpoint;
                }

                if (UsbInterfaceHandler.WriteUsbInterface == null && UsbInterfaceHandler.InterruptUsbInterface?.WriteInterruptEndpoint != null)
                {
                    Logger?.Log(Messages.WarningNoWriteInterfaceFound, nameof(UWPUsbDeviceHandler), null, LogLevel.Warning);
                    UsbInterfaceHandler.WriteUsbInterface = UsbInterfaceHandler.InterruptUsbInterface;
                    UsbInterfaceHandler.WriteUsbInterface.WriteEndpoint = UsbInterfaceHandler.InterruptUsbInterface?.WriteInterruptEndpoint;
                }
            }
            else
            {
                throw new DeviceException(Messages.GetErrorMessageCantConnect(DeviceId));
            }
        }

        protected override IAsyncOperation<windowsUsbDevice> FromIdAsync(string id)
        {
            return windowsUsbDevice.FromIdAsync(id);
        }

        #endregion

        #region Public Methods
        public override void Dispose()
        {
            if (disposed) return;
            disposed = true;

            UsbInterfaceHandler?.Dispose();
            base.Dispose();
        }

        public Task WriteAsync(byte[] data)
        {
            return WriteUsbInterface.WriteAsync(data);
        }

        public Task<ConnectedDeviceDefinitionBase> GetConnectedDeviceDefinitionAsync()
        {
            return Task.FromResult(ConnectedDeviceDefinition);
        }
        #endregion
    }
}