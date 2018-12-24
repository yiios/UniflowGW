using log4net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Licensing
{
    public sealed class KeyStorage
    {
        internal Func<byte[]> Load { get; set; }
        internal Action<byte[]> Store { get; set; }
        private KeyStorage() { }
        public static KeyStorage RegistryKey(string registryKeyPath)
        {
            ILog log = LogManager.GetLogger(typeof(KeyStorage));
            return new KeyStorage()
            {
                Load = () =>
                {
                    try
                    {
                        log.Debug($"KeyStorage.RegistryKey.Load(HKLM/Software/{registryKeyPath})");
                        RegistryKey key = Registry.LocalMachine
                            .OpenSubKey("Software", true)
                            .CreateSubKey(registryKeyPath);
                        if (!key.GetValueNames().Contains("License Data") ||
                            key.GetValueKind("License Data") != RegistryValueKind.Binary)
                            return null;
                        log.Debug("Regedit Key;License Data:" + key.GetValue("License Data"));
                        var data = key.GetValue("License Data") as byte[];
                        return data;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message, ex);
                        throw;
                    }
                },
                Store = (data) =>
                {
                    try
                    {
                        log.Debug($"KeyStorage.RegistryKey.Store(HKLM/Software/{registryKeyPath})");
                        RegistryKey key = Registry.LocalMachine
                            .OpenSubKey("Software", true)
                            .CreateSubKey(registryKeyPath);
                        key.SetValue("License Data", data, RegistryValueKind.Binary);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message, ex);
                        throw;
                    }
                },
            };
        }
        public static KeyStorage File(string filePath)
        {
            ILog log = LogManager.GetLogger(typeof(KeyStorage));
            return new KeyStorage()
            {
                Load = () =>
                {
                    try
                    {
                        log.Debug($"KeyStorage.File.Load({filePath})");
                        var fullPath = System.IO.Path.GetFullPath(filePath);
                        log.Debug($"Full Path: {fullPath}");
                        var dir = System.IO.Path.GetDirectoryName(fullPath);
                        if (!System.IO.Directory.Exists(dir))
                            System.IO.Directory.CreateDirectory(dir);
                        if (!System.IO.File.Exists(filePath))
                            return null;
                        var data = System.IO.File.ReadAllBytes(fullPath);
                        return data;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message, ex);
                        throw;
                    }
                },
                Store = (data) =>
                {
                    try
                    {
                        log.Debug($"KeyStorage.File.Store({filePath})");
                        var fullPath = System.IO.Path.GetFullPath(filePath);
                        log.Debug($"Full Path: {fullPath}");
                        var dir = System.IO.Path.GetDirectoryName(fullPath);
                        if (!System.IO.Directory.Exists(dir))
                            System.IO.Directory.CreateDirectory(dir);
                        System.IO.File.WriteAllBytes(fullPath, data);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message, ex);
                        throw;
                    }
                },
            };
        }
        public static KeyStorage Via(Func<byte[]> load, Action<byte[]> store)
        {
            ILog log = LogManager.GetLogger(typeof(KeyStorage));
            return new KeyStorage()
            {
                Load = () =>
                {
                    log.Debug($"KeyStorage.Via.Load()");
                    try
                    {
                        return load.Invoke();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message, ex);
                        throw;
                    }
                },
                Store = (data) =>
                {
                    log.Debug($"KeyStorage.Via.Store()");
                    try
                    {
                        store.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message, ex);
                        throw;
                    }
                },
            };
        }
    }
}
