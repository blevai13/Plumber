﻿(() => {
    'use strict';

    function workflowActionsService($rootScope, workflowResource, notificationsService) {

        const dialogPath = '../app_plugins/workflow/backoffice/views/dialogs/'; 

        // UI feedback for button directive
        const buttonState = (state, id) => {
            $rootScope.$emit('buttonStateChanged', { state: state, id: id });
        };

        // display notification after actioning workflow task
        const notify = (d, fromDash, id) => {
            if (d.status === 200) {

                notificationsService.success('SUCCESS', d.message);

                if (fromDash) {
                    $rootScope.$emit('refreshWorkflowDash');
                }
                $rootScope.$emit('workflowActioned');
                buttonState('success', id);
            } else {
                notificationsService.error('OH SNAP', d.message);
                buttonState('error', id);
            }
        };

        const service = {

            action: (item, type, fromDash) => {
                let workflowOverlay = {
                    view: dialogPath + 'workflow.action.dialog.html',
                    show: true,
                    title: type + ' workflow process',
                    subtitle: `Document: ${item.nodeName}`,
                    comment: item.comment,
                    approvalComment: '',
                    guid: item.instanceGuid,
                    requestedBy: item.requestedBy,
                    requestedOn: item.requestedOn,
                    submit: model => {

                        buttonState('busy', item.nodeId);

                        // build the function name and access it via index rather than property - saves duplication
                        const functionName = type.toLowerCase() + 'WorkflowTask';
                        workflowResource[functionName](item.instanceGuid, model.approvalComment)
                            .then(resp => {
                                notify(resp, fromDash, item.nodeId);
                            });
                       
                        workflowOverlay.close();
                    },
                    close: () => {
                        workflowOverlay.show = false;
                        workflowOverlay = null;
                    }
                };

                return workflowOverlay;
            },

            initiate: (name, id, publish) => {
                let workflowOverlay = {
                    view: dialogPath + 'workflow.submit.dialog.html',
                    show: true,
                    title: `Send for ${publish ? 'publish' : 'unpublish'} approval`,
                    subtitle: `Document: ${name}`,
                    isPublish: publish,
                    nodeId: id,
                    submit: model => {

                        buttonState('busy', id);

                        workflowResource.initiateWorkflow(id, model.comment, publish)
                            .then(resp => {
                                notify(resp, false, id);
                            });

                        workflowOverlay.close();
                    },
                    close: () => {
                        workflowOverlay.show = false;
                        workflowOverlay = null;
                    }
                };
                return workflowOverlay;
            },

            cancel: (item, fromDash) => {
                let workflowOverlay = {
                    view: dialogPath + 'workflow.cancel.dialog.html',
                    show: true,
                    title: 'Cancel workflow process',
                    subtitle: `Document: ${item.nodeName}`,
                    comment: '',
                    isFinalApproval: item.activeTask === 'Pending Final Approval',
                    submit: model => {

                        buttonState('busy', item.nodeId);

                        workflowResource.cancelWorkflowTask(item.instanceGuid, model.comment)
                            .then(resp => {
                                notify(resp, fromDash, item.nodeId);
                            });

                        workflowOverlay.close();
                    },
                    close: () => {
                        workflowOverlay.show = false;
                        workflowOverlay = null;
                    }
                };

                return workflowOverlay;
            },

            detail: item => {
                let workflowOverlay = {
                    view: dialogPath + 'workflow.action.dialog.html',
                    show: true,
                    title: 'Workflow detail',
                    subtitle: `Document: ${item.nodeName}`,
                    comment: item.comment,
                    guid: item.instanceGuid,
                    requestedBy: item.requestedBy,
                    requestedOn: item.requestedOn,
                    detail: true,
                    
                    close: () => {
                        workflowOverlay.show = false;
                        workflowOverlay = null;
                    }
                };

                return workflowOverlay;
            },

            buttonState: (state, id) => {
                buttonState(state, id);
            }
        };

        return service;
    }

    angular.module('plumber.services').factory('plmbrActionsService',
        ['$rootScope', 'plmbrWorkflowResource', 'notificationsService', workflowActionsService]);

})();