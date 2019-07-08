using System;
// using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
using System.Net;

// using Amazon.SimpleNotificationService;
// using Amazon.SimpleNotificationService.Model;

using System.Threading.Tasks;
using System.IO;
using System.Messaging;
using Microsoft.Win32;
using System.Data;

public partial class Triggers
{
    public static string CLRWriteQueue { get; set; }
    public static string CLRLogQueue { get; set; }



    static MessageQueue CLRQueue = null;
    static MessageQueue CLRLQueue = null;
    //// Enter existing table or view for the target and uncomment the attribute line
    //[Microsoft.SqlServer.Server.SqlTrigger(Name = "SqlTrigger1", Target = "customer", Event = "FOR INSERT, UPDATE, DELETE")]
    public static void SqlTrigger1()
    {
        SqlTriggerContext triggContext = SqlContext.TriggerContext;
        // Replace with your own code
        SqlContext.Pipe.Send("Trigger FIRED");
        SqlCommand command;
        SqlDataReader reader;
        SqlPipe pipe = SqlContext.Pipe;

        switch (triggContext.TriggerAction)
        {
            case TriggerAction.Insert:

                using (SqlConnection conn = new SqlConnection("context connection=true"))
                {
                    conn.Open();
                    command = new SqlCommand(@"SELECT * FROM INSERTED;", conn);
                    reader = command.ExecuteReader();
                    reader.Read();


                    //customerName = (string)reader[1];

                    //pipe.Send(@"You updated: '" + uid + @"' - '"
                    //   + customerName + @"'");

                    //reader.Close();

                    SendMessage(reader, "insert", TransactionType.QuoteInsert);
                }
                break;
            case TriggerAction.Update:
                using (SqlConnection connection
            = new SqlConnection(@"context connection=true"))
                {
                    connection.Open();
                    command = new SqlCommand(@"SELECT * FROM INSERTED;",
                       connection);
                    reader = command.ExecuteReader();
                    reader.Read();

                    //uid = reader[0].ToString();
                    //customerName = (string)reader[1];

                    //pipe.Send(@"You updated: '" + uid + @"' - '"
                    //   + customerName + @"'");

                    //for (int columnNumber = 0; columnNumber < triggContext.ColumnCount; columnNumber++)
                    //{
                    //    pipe.Send("Updated column "
                    //       + reader.GetName(columnNumber) + "? "
                    //       + triggContext.IsUpdatedColumn(columnNumber).ToString());
                    //}

                    //reader.Close();

                    SendMessage(reader, "update", TransactionType.QuoteUpdate);
                }
                break;
            case TriggerAction.Delete:
                using (SqlConnection connection
               = new SqlConnection(@"context connection=true"))
                {
                    connection.Open();
                    command = new SqlCommand(@"SELECT * FROM DELETED;", connection);
                 
                    reader = command.ExecuteReader();
                    
                    if (reader.HasRows)
                    {
                        SendMessage(reader, "deleted", TransactionType.QuoteDelete);
                        reader.Close();
                    }
                    else
                    {
                    }
                }
                break;
        }

    }


    public static void SendMessage(SqlDataReader reader, string desc, Enum enm)
    {
  
        // var view32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
        //var key = view32.OpenSubKey(@"Software\OVIA\CLRTRIGGER\", false);


        CLRLogQueue = @".\private$\quotelq";
        CLRWriteQueue = @".\private$\QouteWQ";
        //File.AppendAllText(@"C:\Users\OVIA\Desktop\text.txt",  "--" + key.GetValue("WriteQueue").ToString());

        QuoteDetails details = new QuoteDetails()
        {
           TABLENAME = string.Empty,
            DESCRIPTION = desc,
            CURRENCYID = Convert.ToInt32(reader["Currency_ID"]),
            QUOTEBUY = Convert.ToDouble(reader["Quote_Buy"]),
            QUOTESELL = Convert.ToDouble(reader["Quote_Sell"]),
            UPDATEDATE = Convert.ToDateTime(reader["Update_Date"]),
            CURRENCYNAME = reader["Currency_Name"].ToString()

        };


        send_app_message(details);

    }


    private static MessageQueue createMSMQ(string queueName, out string error)
    {
        MessageQueue mq = null;
        error = "";
        try
        {
            if (MessageQueue.Exists(queueName))
            {
                mq = new MessageQueue(queueName);
                if (!mq.Transactional)
                {
                    error = "Message queue is not transactional.";
                    mq.Dispose();
                    mq = null;
                }
                //messageQueueForReading.Formatter = new XmlMessageFormatter(new Type[] { typeof(ControlBoardMessage) });
            }
            else
            {
                error = "Message queue does not exist.";
            }
        }
        catch (Exception e)
        {
            error = "Message Queue Exception: " + e.Message;
            mq = null;
        }
        return mq;
    }


    private static void send_app_message(QuoteDetails reader)
    {
        // KIOSK APP MESSAGE

        string error = null;




        CLRQueue = createMSMQ(CLRWriteQueue, out error);
        if (CLRQueue != null)
        {
            System.Messaging.Message m = new System.Messaging.Message(reader);

            m.Label = "QuoteInformation";

            MessageQueueTransaction tr = new MessageQueueTransaction();
            tr.Begin();
            try
            {
                CLRQueue.Send(m, tr);
                tr.Commit();
                send_log_message("SUCCESS");
            }
            catch (Exception ex)
            {
                //  tr.Abort();
                //send_log_message(ex.Message);

            }

            CLRQueue.Dispose();



        }




    }
    private static void send_log_message(string message)
    {
        // KIOSK APP MESSAGE
        string error = null;
        LogMessage lg = new LogMessage()
        {
            DATE = DateTime.Now,
            LOGMESSAGE = message
        };

        CLRLQueue = createMSMQ(CLRLogQueue, out error);
        if (CLRLQueue != null)
        {
            System.Messaging.Message m = new System.Messaging.Message(lg);

            m.Label = "CLRLogMessage";

            MessageQueueTransaction tr = new MessageQueueTransaction();
            tr.Begin();
            try
            {
                CLRLQueue.Send(m, tr);
                tr.Commit();
            }
            catch (Exception ex)
            {
                //  tr.Abort();

            }

            CLRLQueue.Dispose();



        }




    }

    public enum TransactionType
    {
        QuoteUpdate = 1,
        QuoteInsert = 2,
        QuoteDelete = 3

    }
    public partial class QuoteDetails
    {
        public string TABLENAME { get; set; }
        public int CURRENCYID { get; set; }
        public DateTime UPDATEDATE { get; set; }
        public string CURRENCYNAME { get; set; }

        public double QUOTEBUY { get; set; }
        public double QUOTESELL { get; set; }
        public string DESCRIPTION { get; set; }

    }
    public partial class LogMessage
    {
        public string LOGMESSAGE { get; set; }
        public DateTime DATE { get; set; }

    }
}

