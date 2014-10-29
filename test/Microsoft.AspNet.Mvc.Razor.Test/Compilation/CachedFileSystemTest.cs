﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileSystems;
using Microsoft.Framework.Expiration.Interfaces;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CachedFileSystemTest
    {
        private const string FileName = "myView.cshtml";

        public DummyFileSystem TestFileSystem { get; } = new DummyFileSystem();

        public IOptions<RazorViewEngineOptions> OptionsAccessor
        {
            get
            {
                var options = new RazorViewEngineOptions
                {
                    FileSystem = TestFileSystem
                };

                var mock = new Mock<IOptions<RazorViewEngineOptions>>(MockBehavior.Strict);
                mock.Setup(oa => oa.Options).Returns(options);

                return mock.Object;
            }
        }

        public ControllableExpiringFileInfoCache GetCache(IOptions<RazorViewEngineOptions> optionsAccessor)
        {
            return new ControllableExpiringFileInfoCache(optionsAccessor);
        }

        public void CreateFile(string fileName)
        {
            var fileInfo = new DummyFileInfo()
            {
                Name = fileName,
                LastModified = DateTime.Now,
            };

            TestFileSystem.AddFile(fileInfo);
        }

        public void Sleep(ControllableExpiringFileInfoCache cache, int offsetMilliseconds)
        {
            cache.Sleep(offsetMilliseconds);
        }

        public void Sleep(IOptions<RazorViewEngineOptions> accessor, ControllableExpiringFileInfoCache cache, int offsetMilliSeconds)
        {
            var baseMilliSeconds = (int)accessor.Options.ExpirationBeforeCheckingFilesOnDisk.TotalMilliseconds;

            cache.Sleep(baseMilliSeconds + offsetMilliSeconds);
        }

        public void SetExpiration(IOptions<RazorViewEngineOptions> accessor, TimeSpan expiration)
        {
            accessor.Options.ExpirationBeforeCheckingFilesOnDisk = expiration;
        }

        [Fact]
        public void VerifyDefaultOptionsAreSetupCorrectly()
        {
            var optionsAccessor = OptionsAccessor;

            // Assert
            Assert.Equal(2000, optionsAccessor.Options.ExpirationBeforeCheckingFilesOnDisk.TotalMilliseconds);
        }

        [Fact]
        public void GettingFileInfoReturnsTheSameDataWithDefaultOptions()
        {
            // Arrange
            var cache = GetCache(OptionsAccessor);

            CreateFile(FileName);

            // Act
            IFileInfo fileInfo1;
            IFileInfo fileInfo2;
            var result1 = cache.TryGetFileInfo(FileName, out fileInfo1);
            var result2 = cache.TryGetFileInfo(FileName, out fileInfo2);

            // Assert
            Assert.True(result1);
            Assert.True(result2);

            Assert.Same(fileInfo1, fileInfo2);

            Assert.Equal(FileName, fileInfo1.Name);
        }

        [Fact]
        public void GettingFileInfoReturnsTheSameDataWithDefaultOptionsEvenWhenFilesHaveChanged()
        {
            // Arrange
            var cache = GetCache(OptionsAccessor);

            CreateFile(FileName);

            // Act
            var fileInfo1 = cache.GetFileInfo(FileName);

            CreateFile(FileName);

            var fileInfo2 = cache.GetFileInfo(FileName);

            // Assert
            Assert.Same(fileInfo1, fileInfo2);

            Assert.Equal(fileInfo1.LastModified, fileInfo2.LastModified);
            Assert.Equal(FileName, fileInfo1.Name);
            Assert.Equal(FileName, fileInfo2.Name);
        }

        [Fact]
        public void GettingFileInfoReturnsNewDataWithDefaultOptionsAfterExpirationAndFileChange()
        {
            var optionsAccessor = OptionsAccessor;

            // Arrange
            var cache = GetCache(optionsAccessor);

            CreateFile(FileName);

            // Act
            var fileInfo1 = cache.GetFileInfo(FileName);

            Sleep(optionsAccessor, cache, 500);
            CreateFile(FileName);

            var fileInfo2 = cache.GetFileInfo(FileName);

            // Assert
            Assert.NotSame(fileInfo1, fileInfo2);

            Assert.Equal(FileName, fileInfo1.Name);
            Assert.Equal(FileName, fileInfo2.Name);
        }

        [Fact]
        public void GettingFileInfoReturnsNewDataWithDefaultOptionsAfterExpiration()
        {
            // Arrange
            var optionsAccessor = OptionsAccessor;

            var cache = GetCache(optionsAccessor);

            CreateFile(FileName);

            // Act
            var fileInfo1 = cache.GetFileInfo(FileName);

            Sleep(optionsAccessor, cache, 500);

            var fileInfo2 = cache.GetFileInfo(FileName);

            // Assert
            Assert.NotSame(fileInfo1, fileInfo2);

            Assert.Equal(fileInfo1.LastModified, fileInfo2.LastModified);
            Assert.Equal(FileName, fileInfo1.Name);
            Assert.Equal(FileName, fileInfo2.Name);
        }

        public static IEnumerable<object[]> ImmediateExpirationTimespans
        {
            get
            {
                yield return new object[]
                {
                    TimeSpan.FromSeconds(0.0)
                };

                yield return new object[]
                {
                    TimeSpan.FromSeconds(-1.0)
                };

                yield return new object[]
                {
                    TimeSpan.MinValue
                };
            }
        }

        [Theory]
        [MemberData(nameof(ImmediateExpirationTimespans))]
        public void GettingFileInfoReturnsNewDataWithCustomImmediateExpiration(TimeSpan expiration)
        {
            // Arrange
            var optionsAccessor = OptionsAccessor;
            SetExpiration(optionsAccessor, expiration);

            string FileName = "myfile4.cshtml";
            var cache = GetCache(optionsAccessor);

            CreateFile(FileName);

            // Act
            var fileInfo1 = cache.GetFileInfo(FileName);
            var fileInfo2 = cache.GetFileInfo(FileName);

            // Assert
            Assert.NotSame(fileInfo1, fileInfo2);
            Assert.Equal(fileInfo1.LastModified, fileInfo2.LastModified);

            Assert.Equal(FileName, fileInfo1.Name);
            Assert.Equal(FileName, fileInfo2.Name);
        }

        public static IEnumerable<object[]> CustomExpirationTimespans
        {
            get
            {
                yield return new object[]
                {
                    TimeSpan.FromSeconds(1.0)
                };

                yield return new object[]
                {
                    TimeSpan.FromSeconds(3.0)
                };
            }
        }

        [Theory]
        [MemberData(nameof(CustomExpirationTimespans))]
        public void GettingFileInfoReturnsNewDataWithCustomExpiration(TimeSpan expiration)
        {
            // Arrange
            var optionsAccessor = OptionsAccessor;
            SetExpiration(optionsAccessor, expiration);

            string FileName = "myfile5.cshtml";
            var cache = GetCache(optionsAccessor);

            CreateFile(FileName);

            // Act
            var fileInfo1 = cache.GetFileInfo(FileName);

            Sleep(optionsAccessor, cache, 500);

            var fileInfo2 = cache.GetFileInfo(FileName);

            // Assert
            Assert.NotSame(fileInfo1, fileInfo2);

            Assert.Equal(FileName, fileInfo1.Name);
        }

        [Theory]
        [MemberData(nameof(CustomExpirationTimespans))]
        public void GettingFileInfoReturnsSameDataWithCustomExpiration(TimeSpan expiration)
        {
            // Arrange
            var optionsAccessor = OptionsAccessor;
            SetExpiration(optionsAccessor, expiration);

            string FileName = "myfile6.cshtml";
            var cache = GetCache(optionsAccessor);

            CreateFile(FileName);

            // Act
            var fileInfo1 = cache.GetFileInfo(FileName);

            Sleep(optionsAccessor, cache, -500);

            var fileInfo2 = cache.GetFileInfo(FileName);

            // Assert
            Assert.Same(fileInfo1, fileInfo2);

            Assert.Equal(FileName, fileInfo1.Name);
        }

        [Fact]
        public void GettingFileInfoReturnsSameDataWithMaxExpiration()
        {
            // Arrange
            var optionsAccessor = OptionsAccessor;
            SetExpiration(optionsAccessor, TimeSpan.MaxValue);

            string FileName = "myfile7.cshtml";
            var cache = GetCache(optionsAccessor);

            CreateFile(FileName);

            // Act
            var fileInfo1 = cache.GetFileInfo(FileName);

            Sleep(cache, 2500);

            var fileInfo2 = cache.GetFileInfo(FileName);

            // Assert
            Assert.Same(fileInfo1, fileInfo2);

            Assert.Equal(FileName, fileInfo1.Name);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TryGetDirectoryInfo_PassesThroughToUnderlyingFileSystem(bool expected)
        {
            // Arrange
            var fileSystem = new Mock<IFileSystem>();
            var contents = Enumerable.Empty<IFileInfo>();
            fileSystem.Setup(f => f.TryGetDirectoryContents("/test-path", out contents))
                      .Returns(expected)
                      .Verifiable();
            var options = new RazorViewEngineOptions
            {
                FileSystem = fileSystem.Object
            };
            var accessor = new Mock<IOptions<RazorViewEngineOptions>>();
            accessor.SetupGet(a => a.Options)
                    .Returns(options);

            var cachedFileSystem = new CachedFileSystem(accessor.Object);

            // Act
            var result = cachedFileSystem.TryGetDirectoryContents("/test-path", out contents);

            // Assert
            Assert.Equal(expected, result);
            fileSystem.Verify();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TryGetParentPath_PassesThroughToUnderlyingFileSystem(bool expected)
        {
            // Arrange
            var fileSystem = new Mock<IFileSystem>();
            var parentPath = "/";
            fileSystem.Setup(f => f.TryGetParentPath("/test-path", out parentPath))
                      .Returns(expected)
                      .Verifiable();
            var options = new RazorViewEngineOptions
            {
                FileSystem = fileSystem.Object
            };
            var accessor = new Mock<IOptions<RazorViewEngineOptions>>();
            accessor.SetupGet(a => a.Options)
                    .Returns(options);

            var cachedFileSystem = new CachedFileSystem(accessor.Object);

            // Act
            var result = cachedFileSystem.TryGetParentPath("/test-path", out parentPath);

            // Assert
            Assert.Equal(expected, result);
            fileSystem.Verify();
        }

        public class ControllableExpiringFileInfoCache : CachedFileSystem
        {
            public ControllableExpiringFileInfoCache(IOptions<RazorViewEngineOptions> optionsAccessor)
                : base(optionsAccessor)
            {
            }

            private DateTime? _internalUtcNow { get; set; }

            protected override DateTime UtcNow
            {
                get
                {
                    if (_internalUtcNow == null)
                    {
                        _internalUtcNow = base.UtcNow;
                    }

                    return _internalUtcNow.Value.AddTicks(1);
                }
            }

            public void Sleep(int milliSeconds)
            {
                if (milliSeconds <= 0)
                {
                    throw new InvalidOperationException();
                }

                _internalUtcNow = UtcNow.AddMilliseconds(milliSeconds);
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                IFileInfo fileInfo;
                if (TryGetFileInfo(subpath, out fileInfo))
                {
                    return fileInfo;
                }

                return null;
            }
        }

        public class DummyFileSystem : IFileSystem
        {
            private Dictionary<string, IFileInfo> _fileInfos = new Dictionary<string, IFileInfo>(StringComparer.OrdinalIgnoreCase);

            public void AddFile(IFileInfo fileInfo)
            {
                if (_fileInfos.ContainsKey(fileInfo.Name))
                {
                    _fileInfos[fileInfo.Name] = fileInfo;
                }
                else
                {
                    _fileInfos.Add(fileInfo.Name, fileInfo);
                }
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                throw new NotImplementedException();
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                IFileInfo knownInfo;
                if (_fileInfos.TryGetValue(subpath, out knownInfo))
                {
                    return new DummyFileInfo()
                    {
                        Name = knownInfo.Name,
                        LastModified = knownInfo.LastModified,
                    };
                }
                else
                {
                    return new NotFoundFileInfo(subpath);
                }
            }

            public bool TryGetParentPath(string subpath, out string parentPath)
            {
                throw new NotImplementedException();
            }
        }

        public class DummyFileInfo : IFileInfo
        {
            public DateTime LastModified { get; set; }
            public string Name { get; set; }

            public long Length { get { throw new NotImplementedException(); } }
            public bool IsDirectory { get { throw new NotImplementedException(); } }
            public string PhysicalPath { get { throw new NotImplementedException(); } }

            public bool Exists
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Stream CreateReadStream() { throw new NotImplementedException(); }

            public void WriteContent(byte[] content)
            {
                throw new NotImplementedException();
            }

            public void Delete()
            {
                throw new NotImplementedException();
            }

            public IExpirationTrigger CreateFileChangeTrigger()
            {
                throw new NotImplementedException();
            }
        }
    }
}