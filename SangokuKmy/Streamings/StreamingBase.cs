using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data.ApiEntities;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities.Caches;

namespace SangokuKmy.Streamings
{
  public abstract class StreamingBase<EXTRA>
  {
    private readonly List<StreamingData<EXTRA>> streams = new List<StreamingData<EXTRA>>();
    protected readonly ReaderWriterLock locker = new ReaderWriterLock();

    protected void Add(StreamingData<EXTRA> data)
    {
      try
      {
        locker.AcquireWriterLock(30_000);
        this.streams.Add(data);
        locker.ReleaseWriterLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    protected void Add(IEnumerable<StreamingData<EXTRA>> data)
    {
      try
      {
        locker.AcquireWriterLock(30_000);
        foreach (var d in data)
        {
          this.streams.Add(d);
        }
        locker.ReleaseWriterLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    protected IEnumerable<StreamingData<EXTRA>> Where(Func<StreamingData<EXTRA>, bool> subject)
    {
      IEnumerable<StreamingData<EXTRA>> result = null;

      try
      {
        locker.AcquireReaderLock(30_000);
        result = this.streams.Where(subject).ToArray();
        locker.ReleaseReaderLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }

      return result;
    }

    protected void Remove(StreamingData<EXTRA> data)
    {
      try
      {
        locker.AcquireWriterLock(30_000);
        this.streams.Remove(data);
        data.OnRemoved();
        locker.ReleaseWriterLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    protected void Remove(IEnumerable<StreamingData<EXTRA>> data)
    {
      try
      {
        locker.AcquireWriterLock(30_000);
        foreach (var d in data)
        {
          this.streams.Remove(d);
          d.OnRemoved();
        }
        locker.ReleaseWriterLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    private void CleanAbortedResponses()
    {
      var targets = this.streams.Where(s => s.Response.HttpContext.RequestAborted.IsCancellationRequested).ToList();
      if (targets.Any())
      {
        try
        {
          locker.AcquireWriterLock(30_000);
          foreach (var target in targets)
          {
            this.streams.Remove(target);
            target.OnRemoved();
          }
          locker.ReleaseWriterLock();
        }
        catch (ApplicationException ex)
        {
          ErrorCode.LockFailedError.Throw(ex);
        }
      }
    }

    protected async Task SendAsync<T>(ApiData<T> data, Predicate<StreamingData<EXTRA>> subject)
    {
      this.CleanAbortedResponses();
      try
      {
        locker.AcquireReaderLock(30_000);
        var errored = new List<StreamingData<EXTRA>>();
        var value = JsonConvert.SerializeObject(data) + "\n";
        foreach (var d in this.streams.Where(dd => subject(dd)))
        {
          try
          {
            await d.Response.WriteAsync(value);
          }
          catch
          {
            errored.Add(d);
          }
        }
        foreach (var d in errored)
        {
          this.streams.Remove(d);
        }
        locker.ReleaseReaderLock();
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }
  }

  public abstract class CacheStreamingBase<UE> : StreamingBase<UE> where UE : IEntityCache
  {
    protected void AddUnique(StreamingData<UE> data)
    {
      this.Remove(this.Where(cache => cache.ExtraData.Id == data.ExtraData.Id));
      this.Add(data);
    }

    protected void AddUnique(IEnumerable<StreamingData<UE>> data)
    {
      this.Remove(this.Where(cache => data.Any(d => d.ExtraData.Id == cache.ExtraData.Id)));
      this.Add(data);
    }

    protected void UpdateUnique(UE data)
    {
      var c = this.Where(cache => cache.ExtraData.Id == data.Id).SingleOrDefault();
      if (c != null)
      {
        c.ExtraData = data;
      }
    }

    protected void UpdateUnique(IEnumerable<UE> data)
    {
      foreach (var d in this.Where(cache => data.Any(dd => dd.Id == cache.ExtraData.Id)).ToArray())
      {
        var cache = data.SingleOrDefault(dd => dd.Id == d.ExtraData.Id);
        if (cache != null)
        {
          d.ExtraData = cache;
        }
      }
    }
  }

  public abstract class StreamingBase : StreamingBase<object> { }

  public class StreamingData<EXTRA>
  {
    public AuthenticationData AuthData { get; set; }
    public HttpResponse Response { get; set; }
    public EXTRA ExtraData { get; set; }

    public void OnRemoved()
    {
      this.Removed?.Invoke(this, EventArgs.Empty);
      this.Removed = null;
    }

    public event EventHandler Removed;
  }
}
