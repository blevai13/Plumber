﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Hosting;
using Moq;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Routing;
using Umbraco.Web.Security;
using Workflow.Models;
using Workflow.Services;
using Workflow.Services.Interfaces;

namespace Workflow.Tests
{
    public static class Scaffold
    {
        private static readonly IInstancesService InstancesService = new InstancesService();

        /// <summary>
        /// 
        /// </summary>
        public static void Run()
        {
            Tables();
            EnsureContext();
        }

        public static void Tables()
        {
            // ensure required tables exist
            DatabaseSchemaHelper persistenceHelper = Persistence.Helper();

            persistenceHelper.CreateTable<UserGroupPoco>();
            persistenceHelper.CreateTable<User2UserGroupPoco>();
            persistenceHelper.CreateTable<UserGroupPermissionsPoco>();
            persistenceHelper.CreateTable<WorkflowSettingsPoco>();
            persistenceHelper.CreateTable<WorkflowInstancePoco>();
            persistenceHelper.CreateTable<WorkflowTaskInstancePoco>();

        }

        public static void EnsureContext()
        {
            if (UmbracoContext.Current != null)
            {
                return;
            }

            var request = new SimpleWorkerRequest("", "", "", null, new StringWriter());
            var httpContext = new HttpContextWrapper( new HttpContext(request));

            Mock<WebSecurity> webSecurity = new Mock<WebSecurity>(httpContext, ApplicationContext.Current);
            var currentUser = Mock.Of<IUser>(u =>
                    u.IsApproved
                    && u.Name == Utility.RandomString()
                    && u.Id == Utility.CurrentUserId);

            webSecurity.Setup(x => x.CurrentUser).Returns(currentUser);

            UmbracoContext.EnsureContext(
                httpContext,
                ApplicationContext.Current,
                webSecurity.Object,
                UmbracoConfig.For.UmbracoSettings(),
                Mock.Of<IEnumerable<IUrlProvider>>(),
                false);
        }

        /// <summary>
        /// Import the workflow config from the json file
        /// </summary>
        public static void Config()
        {
            var service = new ImportExportService();
            var model = ReadFromJsonFile<ImportExportModel>(@"Config.json");

            service.Import(model);
        }

        /// <summary>
        /// Add a simple content tree to the install
        /// </summary>
        public static void ContentType(IContentTypeService contentTypeService, string name = "Textpage")
        {
            var type = new ContentType(-1)
            {
                Alias = name.ToLower(),
                Name = name,
                AllowedAsRoot = true
            };

            contentTypeService.Save(type);
        }

        /// <summary>
        /// Add a simple content tree to the install
        /// </summary>
        public static IContent Node(IContentService contentService, int parentId = -1, string typeAlias = "textpage")
        {
            IContent node = contentService.CreateContent(Utility.RandomString(), parentId, typeAlias);
            contentService.SaveAndPublishWithStatus(node);

            return node;
        }

        public static Dictionary<int, List<UserGroupPermissionsPoco>> Permissions(int id, int group, int perm, int contentTypeId = 0)
        {
            // set permissions on root
            // mock some data
            return new Dictionary<int, List<UserGroupPermissionsPoco>>
            {
                [id] = new List<UserGroupPermissionsPoco>
                {
                    new UserGroupPermissionsPoco
                    {
                        NodeId = id,
                        GroupId = group,
                        ContentTypeId = contentTypeId,
                        Permission = perm
                    }
                }
            };
        }

        public static User2UserGroupPoco GetUser2UserGroupPoco(int groupId)
        {
            int id = Utility.RandomInt();

            return new User2UserGroupPoco
            {
                GroupId = groupId,
                Id = id,
                UserId = id
            };
        }

        /// <summary>
        /// Quickly scaffold a set of instances with arbitrary values and no tasks
        /// This method adds the instances to the db
        /// </summary>
        /// <param name="count"></param>
        /// <param name="type"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static IEnumerable<WorkflowInstancePoco> Instances(int count, int type = 1, int status = (int)WorkflowStatus.PendingApproval, int? nodeId = null)
        {
            List<WorkflowInstancePoco> response = new List<WorkflowInstancePoco>();

            for (var i = 0; i < count; i++)
            {
                WorkflowInstancePoco instance = Instance(Guid.NewGuid(), type, nodeId ?? Utility.RandomInt(), Utility.RandomInt(), status);

                InstancesService.InsertInstance(instance);
                response.Add(instance);
            }

            return response;
        }

        public static WorkflowInstancePoco Instance(Guid guid, int type, int nodeId = 1073, int authorUserId = 0, int status = (int)WorkflowStatus.PendingApproval)
        {
            return new WorkflowInstancePoco
            {
                AuthorUserId = authorUserId,
                Guid = guid,
                AuthorComment = Utility.RandomString(),
                NodeId = nodeId,
                CreatedDate = DateTime.Now,
                Type = type,
                Status = status
            };
        }

        public static WorkflowTaskInstancePoco Task(Guid guid = new Guid(), DateTime createdDate = new DateTime(), int groupId = 1, int approvalStep = 1, int status = 3)
        {
            return new WorkflowTaskInstancePoco
            {
                GroupId = groupId,
                Comment = Utility.RandomString(),
                CreatedDate = createdDate == DateTime.MinValue ? DateTime.Now : createdDate,
                ApprovalStep = approvalStep,
                WorkflowInstanceGuid = guid == Guid.Empty ? Guid.NewGuid() : guid,
                Status = status
            };
        }

        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                string fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(fileContents);
            }
            finally
            {
                reader?.Close();
            }
        }
    }
}
