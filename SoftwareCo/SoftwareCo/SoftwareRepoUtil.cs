﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class SoftwareRepoUtil
    {
        private SoftwareCoUtil _util;

        public SoftwareRepoUtil()
        {
            _util = new SoftwareCoUtil();
        }

        public class RepoCommitChanges
        {
            public int insertions = 0;
            public int deletions = 0;
            public RepoCommitChanges(int insertions, int deletions)
            {
                this.insertions = insertions;
                this.deletions = deletions;
            }
        }

        public class RepoCommit
        {
            public string commitId = "";
            public string message = "";
            public long timestamp = 0L;
            public string date = "";
            public IDictionary<string, RepoCommitChanges> changes = new Dictionary<string, RepoCommitChanges>();

            public RepoCommit(string commitId, string message, long timestamp)
            {
                this.commitId = commitId;
                this.message = message;
                this.timestamp = timestamp;
            }
        }

        public class RepoMember
        {
            public string name = "";
            public string email = "";
            public RepoMember(string name, string email)
            {
                this.name = name;
                this.email = email;
            }
        }

        public class RepoData
        {
            public string identifier = "";
            public string tag = "";
            public string branch = "";
            public List<RepoMember> members = new List<RepoMember>();
            public RepoData(string identifier, string tag, string branch, List<RepoMember> members)
            {
                this.identifier = identifier;
                this.tag = tag;
                this.branch = branch;
                this.members = members;
            }
        }

        public class RepoCommitData
        {
            public string identifier = "";
            public string tag = "";
            public string branch = "";
            public List<RepoCommit> commits = new List<RepoCommit>();
            public RepoCommitData(string identifier, string tag, string branch, List<RepoCommit> commits)
            {
                this.identifier = identifier;
                this.tag = tag;
                this.branch = branch;
                this.commits = commits;
            }
        }

        public IDictionary<string, string> GetResourceInfo(string projectDir)
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();
            string identifier = _util.RunCommand("git config remote.origin.url", projectDir);
            if (identifier != null && !identifier.Equals(""))
            {
                dict.Add("identifier", identifier);

                // only get these since identifier is available
                string email = _util.RunCommand("git config user.email", projectDir);
                if (email != null && !email.Equals(""))
                {
                    dict.Add("email", email);
                }
                string branch = _util.RunCommand("git symbolic-ref --short HEAD", projectDir);
                if (branch != null && !branch.Equals(""))
                {
                    dict.Add("branch", branch);
                }
                string tag = _util.RunCommand("git describe --all", projectDir);

                if (tag != null && !tag.Equals(""))
                {
                    dict.Add("tag", tag);
                }
            }

            return dict;
        }

        public async void GetRepoUsers(string projectDir)
        {
            if (projectDir == null || projectDir.Equals(""))
            {
                return;
            }

            IDictionary<string, string> resourceInfo = this.GetResourceInfo(projectDir);

            if (resourceInfo != null && resourceInfo.ContainsKey("identifier"))
            {
                string identifier = "";
                resourceInfo.TryGetValue("identifier", out identifier);
                if (identifier != null && !identifier.Equals(""))
                {
                    string tag = "";
                    resourceInfo.TryGetValue("tag", out tag);
                    string branch = "";
                    resourceInfo.TryGetValue("branch", out branch);

                    string gitLogData = _util.RunCommand("git log --pretty=%an,%ae | sort", projectDir);

                    IDictionary<string, string> memberMap = new Dictionary<string, string>();

                    List<RepoMember> repoMembers = new List<RepoMember>();
                    if (gitLogData != null && !gitLogData.Equals(""))
                    {
                        string[] lines = gitLogData.Split(
                            new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines != null && lines.Length > 0)
                        {
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string line = lines[i];
                                string[] memberInfos = line.Split(',');
                                if (memberInfos != null && memberInfos.Length > 1)
                                {
                                    string name = memberInfos[0].Trim();
                                    string email = memberInfos[1].Trim();
                                    if (!memberMap.ContainsKey(email))
                                    {
                                        memberMap.Add(email, name);
                                        repoMembers.Add(new RepoMember(name, email));
                                    }
                                }
                            }
                        }
                    }

                    if (memberMap.Count > 0)
                    {
                        RepoData repoData = new RepoData(identifier, tag, branch, repoMembers);
                        string jsonContent = SimpleJson.SerializeObject(repoData);
                        // send the members
                        HttpResponseMessage response = await _util.SendRequestAsync(
                            HttpMethod.Post, "/repo/members", jsonContent);

                        if (!_util.IsOk(response))
                        {
                            Logger.Info(response.ToString());
                        } else
                        {
                            Logger.Error(response.ToString());
                        }
                    }
                }
            }
        }

        /**
         * Get the latest repo commit
         **/
        public async Task<RepoCommit> GetLatestCommitAsync(string projectDir)
        {
            if (projectDir == null || projectDir.Equals(""))
            {
                return null;
            }

            IDictionary<string, string> resourceInfo = this.GetResourceInfo(projectDir);

            if (resourceInfo != null && resourceInfo.ContainsKey("identifier"))
            {
                string identifier = "";
                resourceInfo.TryGetValue("identifier", out identifier);
                if (identifier != null && !identifier.Equals(""))
                {
                    string tag = "";
                    resourceInfo.TryGetValue("tag", out tag);
                    string branch = "";
                    resourceInfo.TryGetValue("branch", out branch);

                    string qryString = "?identifier=" + identifier;
                    qryString += "&tag=" + tag;
                    qryString += "&branch=" + branch;

                    HttpResponseMessage response = await _util.SendRequestAsync(
                            HttpMethod.Get, "/commits/latest?" + qryString, null);

                    if (_util.IsOk(response))
                    {

                        // get the json data
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);

                        jsonObj.TryGetValue("commitId", out object commitIdObj);
                        string commitId = (commitIdObj == null) ? "" : Convert.ToString(commitIdObj);

                        jsonObj.TryGetValue("message", out object messageObj);
                        string message = (messageObj == null) ? "" : Convert.ToString(messageObj);

                        jsonObj.TryGetValue("message", out object timestampObj);
                        long timestamp = (timestampObj == null) ? 0L : Convert.ToInt64(timestampObj);

                        RepoCommit repoCommit = new RepoCommit(commitId, message, timestamp);
                        return repoCommit;
                    }
                }
            }
            return null;
        }

        public async void GetHistoricalCommitsAsync(string projectDir)
        {
            if (projectDir == null || projectDir.Equals(""))
            {
                return;
            }

            IDictionary<string, string> resourceInfo = this.GetResourceInfo(projectDir);

            if (resourceInfo != null && resourceInfo.ContainsKey("identifier"))
            {
                string identifier = "";
                resourceInfo.TryGetValue("identifier", out identifier);
                if (identifier != null && !identifier.Equals(""))
                {
                    string tag = "";
                    resourceInfo.TryGetValue("tag", out tag);
                    string branch = "";
                    resourceInfo.TryGetValue("branch", out branch);
                    string email = "";
                    resourceInfo.TryGetValue("email", out email);

                    RepoCommit latestCommit = await this.GetLatestCommitAsync(projectDir);

                    string sinceOption = "";
                    if (latestCommit != null)
                    {
                        sinceOption = " --since=" + latestCommit.timestamp;
                    }

                    string cmd = "git log --stat --pretty=COMMIT:%H,%ct,%cI,%s --author=" + email + "" + sinceOption;

                    string gitCommitData = _util.RunCommand(cmd, projectDir);

                    if (gitCommitData != null && !gitCommitData.Equals(""))
                    {
                        string[] lines = gitCommitData.Split(
                            new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        RepoCommit currentRepoCommit = null;
                        List<RepoCommit> repoCommits = new List<RepoCommit>();
                        if (lines != null && lines.Length > 0)
                        {
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string line = lines[i].Trim();
                                if (line.Length > 0)
                                {
                                    if (line.IndexOf("COMMIT:") == 0)
                                    {
                                        line = line.Substring("COMMIT:".Length);
                                        if (currentRepoCommit != null)
                                        {
                                            repoCommits.Add(currentRepoCommit);
                                        }

                                        string[] commitInfos = line.Split(',');
                                        if (commitInfos != null && commitInfos.Length > 0)
                                        {
                                            string commitId = commitInfos[0].Trim();
                                            // go to the next line if we've already processed this commitId
                                            if (latestCommit != null && commitId.Equals(latestCommit.commitId))
                                            {
                                                currentRepoCommit = null;
                                                continue;
                                            }

                                            // get the other attributes now
                                            long timestamp = Convert.ToInt64(commitInfos[1].Trim());
                                            string date = commitInfos[2].Trim();
                                            string message = commitInfos[3].Trim();
                                            currentRepoCommit = new RepoCommit(commitId, message, timestamp);
                                            currentRepoCommit.date = date;

                                            RepoCommitChanges changesObj = new RepoCommitChanges(0, 0);
                                            currentRepoCommit.changes.Add("__sftwTotal__", changesObj);
                                        }
                                    }
                                }
                                else if (currentRepoCommit != null && line.IndexOf("|") != -1)
                                {
                                    // get the file and changes
                                    // i.e. somefile.cs                             | 20 +++++++++---------
                                    line = string.Join(" ", line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ));
                                    string[] lineInfos = line.Split('|');
                                    if (lineInfos != null && lineInfos.Length > 1)
                                    {
                                        string file = lineInfos[0].Trim();
                                        string[] metricInfos = lineInfos[1].Trim().Split(' ');
                                        if (metricInfos != null && metricInfos.Length > 1)
                                        {
                                            string addAndDeletes = metricInfos[1].Trim();
                                            int len = addAndDeletes.Length;
                                            int lastPlusIdx = addAndDeletes.LastIndexOf('+');
                                            int insertions = 0;
                                            int deletions = 0;
                                            if (lastPlusIdx != -1)
                                            {
                                                insertions = lastPlusIdx + 1;
                                                deletions = len - insertions;
                                            } else if (len > 0)
                                            {
                                                // all deletions
                                                deletions = len;
                                            }
                                            RepoCommitChanges changesObj = new RepoCommitChanges(insertions, deletions);
                                            currentRepoCommit.changes.Add(file, changesObj);

                                            RepoCommitChanges totalRepoCommit;
                                            currentRepoCommit.changes.TryGetValue("__sftwTotal__", out totalRepoCommit);
                                            if (totalRepoCommit != null)
                                            {
                                                totalRepoCommit.deletions += deletions;
                                                totalRepoCommit.insertions += insertions;
                                                currentRepoCommit.changes.Add("__sftwTotal__", totalRepoCommit);

                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (currentRepoCommit != null)
                        {
                            repoCommits.Add(currentRepoCommit);
                        }

                        if (repoCommits != null && repoCommits.Count > 0)
                        {
                            List<RepoCommit> batch = new List<RepoCommit>();
                            for (int i = 0; i < repoCommits.Count; i++)
                            {
                                batch.Add(repoCommits[i]);
                                if (i > 0 && i % 100 == 0)
                                {
                                    // send this batch
                                    RepoCommitData commitData = new RepoCommitData(identifier, tag, branch, batch);

                                    string jsonContent = SimpleJson.SerializeObject(commitData);
                                    // send the members
                                    HttpResponseMessage response = await _util.SendRequestAsync(
                                        HttpMethod.Post, "/commits", jsonContent);

                                    if (!_util.IsOk(response))
                                    {
                                        Logger.Info(response.ToString());
                                    }
                                    else
                                    {
                                        Logger.Error(response.ToString());
                                    }
                                }
                            }

                            if (batch.Count > 0)
                            {
                                RepoCommitData commitData = new RepoCommitData(identifier, tag, branch, batch);

                                string jsonContent = SimpleJson.SerializeObject(commitData);
                                // send the members
                                HttpResponseMessage response = await _util.SendRequestAsync(
                                    HttpMethod.Post, "/commits", jsonContent);

                                if (!_util.IsOk(response))
                                {
                                    Logger.Info(response.ToString());
                                }
                                else
                                {
                                    Logger.Error(response.ToString());
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}