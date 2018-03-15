﻿using System;
using System.Collections.Generic;

namespace Ooui
{
    public abstract class Session
    {
        protected readonly Element element;
        protected readonly double initialWidth;
        protected readonly double initialHeight;

        protected readonly HashSet<string> createdIds;

        protected readonly List<Message> queuedMessages = new List<Message> ();

        public Session (Element element, double initialWidth, double initialHeight)
        {
            this.element = element;
            this.initialWidth = initialWidth;
            this.initialHeight = initialHeight;

            //
            // Keep a list of all the elements for which we've transmitted the initial state
            //
            createdIds = new HashSet<string> {
                "window",
                "document",
                "document.body",
            };
        }

        void QueueStateMessagesLocked (EventTarget target)
        {
            if (target == null) return;
            var created = false;
            foreach (var m in target.StateMessages) {
                if (m.MessageType == MessageType.Create) {
                    createdIds.Add (m.TargetId);
                    created = true;
                }
                if (created) {
                    QueueMessageLocked (m);
                }
            }
        }

        protected void QueueMessageLocked (Message message)
        {
            //
            // Make sure all the referenced objects have been created
            //
            if (!createdIds.Contains (message.TargetId)) {
                QueueStateMessagesLocked (element.GetElementById (message.TargetId));
            }
            if (message.Value is EventTarget ve) {
                if (!createdIds.Contains (ve.Id)) {
                    QueueStateMessagesLocked (ve);
                }
            }
            else if (message.Value is Array a) {
                for (var i = 0; i < a.Length; i++) {
                    // Console.WriteLine ($"A{i} = {a.GetValue(i)}");
                    if (a.GetValue (i) is EventTarget e && !createdIds.Contains (e.Id)) {
                        QueueStateMessagesLocked (e);
                    }
                }
            }

            //
            // Add it to the queue
            //
            //Console.WriteLine ($"QM {message.MessageType} {message.TargetId} {message.Key} {message.Value}");
            queuedMessages.Add (message);
        }

        protected virtual void QueueMessage (Message message)
        {
            lock (queuedMessages) {
                QueueMessageLocked (message);
            }
        }

        protected void Error (string message, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine ("{0}: {1}", message, ex);
            Console.ResetColor ();
        }

        protected void Info (string message)
        {
            Console.WriteLine (message);
        }
    }
}
