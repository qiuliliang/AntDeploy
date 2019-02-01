﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntDeployAgentWindows.Model;
using AntDeployAgentWindows.WebApiCore;
using Newtonsoft.Json;

namespace AntDeployAgentWindows.MyApp
{
    public class LoggerService: BaseWebApi
    {
        public static ConcurrentDictionary<string,List<LoggerModel>>  loggerCollection = new ConcurrentDictionary<string, List<LoggerModel>>();
        public static ConcurrentQueue<string> removeLoggerConllection = new ConcurrentQueue<string>();


        private static readonly System.Threading.Timer mDetectionTimer;

        static LoggerService()
        {
            mDetectionTimer = new System.Threading.Timer(OnVerifyClients , null, 1000*60*10, 1000*60*10);
        }

        protected override void ProcessRequest()
        {
            Response.ContentType = "text/plain";
            if (Request.Method.ToUpper() != "GET")
            {
                Response.Write("");
                return;
            }

            var key = Request.Query.Get("key");
            if (string.IsNullOrEmpty(key))
            {
                Response.Write("key required");
                return;
            }

            var now = DateTime.Now;
            if (loggerCollection.TryGetValue(key, out List<LoggerModel> logList))
            {
                if (logList != null)
                {
                    var result = logList.Where(r => !r.IsActive && r.Date <= now).ToList();
                    if (result.Any())
                    {
                        result.ForEach(r=>r.IsActive=true);
                        Response.Write(JsonConvert.SerializeObject(result));
                        return;
                    }
                }
            }
           
            Response.Write("");
        }


        public static void Remove(string key)
        {
            removeLoggerConllection.Enqueue(key);
        }

        private static void OnVerifyClients(object state)
        {
            mDetectionTimer.Change(-1, -1);
            try
            {
                if (removeLoggerConllection.TryDequeue(out var key))
                {
                    loggerCollection.TryRemove(key, out _);
                }
            }
            catch { }
            finally
            {
                mDetectionTimer.Change(1000*60*10, 1000*60*10);
            }
        }

    }


    
}