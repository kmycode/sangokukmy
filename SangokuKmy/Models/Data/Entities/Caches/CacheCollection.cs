using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;

namespace SangokuKmy.Models.Data.Entities.Caches
{
  public class CacheCollection<T> where T : class, IEntityCache
  {
    private readonly List<T> caches = new List<T>();
    private readonly ReaderWriterLock locker = new ReaderWriterLock();

    public void Append(T item) => this.Append(new T[] { item });

    public void Append(IEnumerable<T> items)
    {
      try
      {
        locker.AcquireWriterLock(30_000);

        foreach (var item in items)
        {
          var old = this.caches.SingleOrDefault(c => c.Id == item.Id);
          if (old != null)
          {
            this.caches.Remove(old);
          }
          this.caches.Add(item);
        }

        locker.ReleaseWriterLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    public void Clear()
    {
      try
      {
        locker.AcquireWriterLock(30_000);

        this.caches.Clear();

        locker.ReleaseWriterLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    public Optional<T> Find(uint id) => this.Find((obj) => obj.Id == id).SingleOrDefault().ToOptional();

    public IEnumerable<T> Find(Func<T, bool> subject)
    {
      IEnumerable<T> result = null;

      try
      {
        locker.AcquireReaderLock(30_000);

        result = this.caches.Where(subject).ToArray();

        locker.ReleaseReaderLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }

      return result;
    }
  }
}
