namespace ImpliciX.SharedKernel.SerialApi2.SerialPort
{
    internal enum SysErrNo
    {
        /// <summary>
        /// No error detected.
        /// </summary>
        NETFX_OK = 0,

        /// <summary>
        /// ArgumentException
        /// </summary>
        NETFX_EINVAL = 1,

        /// <summary>
        /// UnauthorizedAccessException
        /// </summary>
        NETFX_EACCES = 2,

        /// <summary>
        /// OutOfMemoryException
        /// </summary>
        NETFX_ENOMEM = 3,

        /// <summary>
        /// InvalidOperationException
        /// </summary>
        NETFX_EBADF = 4,

        /// <summary>
        /// PlatformNotSupportedException
        /// </summary>
        NETFX_ENOSYS = 5,

        /// <summary>
        /// IOException
        /// </summary>
        NETFX_EIO = 6,

        /// <summary>
        /// No error.
        /// </summary>
        NETFX_EAGAIN = 7,

        /// <summary>
        /// No error.
        /// </summary>
        NETFX_EWOULDBLOCK = 8,

        /// <summary>
        /// No error.
        /// </summary>
        NETFX_EINTR = 9,

        /// <summary>
        /// Unmapped error. InvalidOperationException
        /// </summary>
        NETFX_UNKNOWN = -1
    }
}