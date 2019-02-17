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
using System.Text;
using Nito.AsyncEx;

namespace SangokuKmy.Streamings
{
  public abstract class StreamingBase<EXTRA>
  {
    private readonly List<StreamingData<EXTRA>> streams = new List<StreamingData<EXTRA>>();
    private readonly AsyncReaderWriterLock locker = new AsyncReaderWriterLock();

    protected void Add(StreamingData<EXTRA> data)
    {
      try
      {
        using (locker.WriterLock())
        {
          this.streams.Add(data);
        }
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
        using (locker.WriterLock())
        {
          foreach (var d in data)
          {
            this.streams.Add(d);
          }
        }
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
        using (locker.ReaderLock())
        {
          result = this.streams.Where(subject).ToArray();
        }
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }

      return result;
    }

    protected void Remove(Predicate<StreamingData<EXTRA>> subject)
    {
      try
      {
        using (locker.WriterLock())
        {
          var targets = this.streams.Where(s => subject(s)).ToArray();
          foreach (var t in targets)
          {
            this.streams.Remove(t);
            t.OnRemoved();
          }
        }
      }
      catch (ApplicationException ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    protected void Remove(StreamingData<EXTRA> data)
    {
      try
      {
        using (locker.WriterLock())
        {
          this.streams.Remove(data);
          data.OnRemoved();
        }
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
        using (locker.WriterLock())
        {
          foreach (var d in data)
          {
            this.streams.Remove(d);
            d.OnRemoved();
          }
        }
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
          using (locker.WriterLock())
          {
            foreach (var target in targets)
            {
              this.streams.Remove(target);
              target.OnRemoved();
            }
          }
        }
        catch (ApplicationException ex)
        {
          ErrorCode.LockFailedError.Throw(ex);
        }
      }
    }

    protected async Task SendAsync<T>(ApiData<T> data, Predicate<StreamingData<EXTRA>> subject)
    {
      await this.SendAsync(new ApiData<T>[] { data }, subject);
    }

    protected async Task SendAsync(IEnumerable<IApiData> data, Predicate<StreamingData<EXTRA>> subject)
    {
      this.CleanAbortedResponses();
      try
      {
        using (locker.ReaderLock())
        {
          var errored = new List<StreamingData<EXTRA>>();
          var values = new StringBuilder();
          values.AppendJoin("\n", data.Select(d => JsonConvert.SerializeObject(d)));
          values.Append("\n");

          var targets = this.streams.Where(dd => subject(dd)).ToArray();
          async Task sendFunc(StreamingData<EXTRA> d)
          {
            try
            {
              await d.Response.WriteAsync(values.ToString());
            }
            catch
            {
              errored.Add(d);
            }
          }
          if (targets.Count() > 3)
          {
            await Task.WhenAll(targets.Select(d => Task.Run(async () => await sendFunc(d))).ToArray());
          }
          else
          {
            foreach (var d in targets)
            {
              await sendFunc(d);
            }
          }

          if (errored.Any())
          {
            using (locker.UpgradeableReaderLock())
            {
              foreach (var d in errored)
              {
                this.streams.Remove(d);
              }
            }
          }
        }
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
        d.ExtraData = cache;
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
