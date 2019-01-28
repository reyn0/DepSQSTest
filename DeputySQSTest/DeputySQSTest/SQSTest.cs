using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Deputy
{
    /// <summary>
    /// Before we run this, make sure to start elasticmq server by running this at command prompt:
    /// java -jar elasticmq-server-0.14.6.jar
    /// 
    /// Then go to http://localhost:9324/ in the web browser to make sure the server is run
    /// 
    /// Once you finished with the test, you can turn off the command prompt by Ctrl+C
    /// </summary>
    [TestClass]
    public class SQSTest
    {
        //Setting up the commonly used variables
        AmazonSQSConfig sqsConfig = new AmazonSQSConfig();
        AmazonSQSClient sqsClient;
        private const string prefix = "TestQueue";
        private string queueName;
        private Task<string> queueURL;
        private string messageBody = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
        private string messageReceipt;

        #region Test Setup 
        /// <summary>
        /// Initialize the test and set the config to point to elasticmq
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            // Set the service URL to point to elasticmq URL
            sqsConfig.ServiceURL = "http://localhost:9324/";

            // Create a new sqs client
            sqsClient = new AmazonSQSClient("x", "x", sqsConfig);
        }

        /// <summary>
        /// Clean up the queue after every test. 
        /// Make sure that we delete every queue created by this test run
        /// </summary>
        [TestCleanup]
        public void SQSCleanUp()
        {
            var sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            foreach (string queue in sqsQueueList.Result.QueueUrls)
            {
                if (queue.Contains(prefix))
                {
                    try
                    {
                        sqsClient.DeleteQueueAsync(queue);
                    }
                    catch (Exception)
                    {
                        Console.Write("Failed to clean up queue {0}", queue);
                    }
                }
            }
        }
        #endregion

        #region EndToEndTest
        ///<summary>
        /// This will test the end to end journey of a user
        /// Steps:
        /// 1. Create the queue
        /// 2. Verify that the queue is created and the list is not null
        /// 3. Send message to the queue
        /// 4. Verify the message body is correct between the request and the response
        /// 5. Receive the message that just being sent
        /// 6. Verify the message body is the same
        /// 7. Get message receipt handler
        /// 8. Delete the message
        /// 9. Verify that message is deleted from the queue
        /// 10. Since we know that we only created 1 queue, we can delete the only one we created
        /// 11. Verify that the list count is now 0
        ///</summary>
        [TestMethod]
        [Ignore("Ignore this test due to incomplete code. Error on sending the message")]
        public async Task TestEndToEnd()
        {
            // Create the queue
            queueName = setQueueName();
            CreateQueueResponse createResponse = await sqsClient.CreateQueueAsync(queueName);

            // Get the queue list. Assert is added to make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 1, "Queue is not created or more than one queue is created");

            // Verify the response created from when creating the queue is the same as the one we get from get queue URL
            var request = new GetQueueUrlRequest(queueName);
            var responseQueue = sqsClient.GetQueueUrlAsync(request);
            Assert.AreEqual(createResponse.QueueUrl, responseQueue.Result.QueueUrl, "The queue URL is not the same");

            // Send message to the queue
            var requestMessage = new SendMessageRequest(responseQueue.Result.ToString(), messageBody);

            // Get the response when sending the message request
            var responseMessage = await sqsClient.SendMessageAsync(requestMessage);

            // Verify the message body is correct between the request and the response
            ValidateMD5(requestMessage.MessageBody, responseMessage.MD5OfMessageBody);

            // Since we know that we only created 1 queue, we can delete the only one
            await sqsClient.DeleteQueueAsync(sqsQueueList.QueueUrls[0].ToString());

            // Get the list again, and verify that the list count is now 0
            sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 0, "The queue is not deleted");
        }
        #endregion

        #region Positive Test
        /// <summary>
        /// Create the queue 
        /// Steps:
        /// 1. Create the queue
        /// 2. Verify that the queue is created and the list is not null
        /// 3. Verify the create response url and the queue url is the same
        /// </summary>
        [TestMethod]
        public async Task TestCreateQueueAsync()
        {
            // Create the queue
            queueName = setQueueName();
            CreateQueueResponse createResponse = await sqsClient.CreateQueueAsync(queueName);

            // Get the queue list. Assert is added to make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.Result.QueueUrls.Count == 1, "Queue is not created or more than one queue is created");

            // Verify the response created from when creating the queue is the same as the one we get from get queue URL
            var request = new GetQueueUrlRequest(queueName);
            var response = sqsClient.GetQueueUrlAsync(request);
            Assert.AreEqual(createResponse.QueueUrl, response.Result.QueueUrl, "The queue URL is not the same");
        }

        /// <summary>
        /// Delete the queue
        /// Steps:
        /// 1. Create the queue
        /// 2. Since we know that we only created 1 queue, we can delete the only one we created
        /// 3. Verify that the list count is now 0
        /// </summary>
        [TestMethod]
        public async Task TestDeleteQueueAsync()
        {
            // Create the queue 
            queueName = setQueueName();
            await sqsClient.CreateQueueAsync(queueName);

            // Get the list and make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = await sqsClient.ListQueuesAsync(queueName);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 1, "Queue is not created or more than one queue is created");

            // Since we know that we only created 1 queue, we can delete the only one
            await sqsClient.DeleteQueueAsync(sqsQueueList.QueueUrls[0].ToString());

            // Get the list again, and verify that the list count is now 0
            sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 0, "The queue is not deleted");
        }

        /// <summary>
        /// Test Get Queue URL
        /// Steps:
        /// 1. Create the queue
        /// 2. Get queue URL
        /// </summary>
        [TestMethod]
        public void TestGetQueueURL()
        {
            // Create the queue 
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            string expectedQueueURL = "http://localhost:9324/queue/" + queueName;

            Assert.AreEqual(expectedQueueURL, queueURL.Result.ToString(), "Queue URL is not the same");
        }

        /// <summary>
        /// Send message in a queue
        /// Steps:
        /// 1. Create the queue
        /// 2. Send message to the queue
        /// 3. Verify the message body is correct between the request and the response
        /// </summary>
        [TestMethod]
        public void TestSendMessage()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            // Send message to the queue
            var request = new SendMessageRequest(queueURL.Result.ToString(), messageBody);

            // Get the response when sending the message request
            var response = sqsClient.SendMessageAsync(request);

            // Verify the message body is correct between the request and the response
            ValidateMD5(request.MessageBody, response.Result.MD5OfMessageBody);
        }

        /// <summary>
        /// Send message in a queue
        /// Steps:
        /// 1. Create the queue
        /// 2. Send message to the queue
        /// 3. Verify the message body is correct between the request and the response
        /// </summary>
        [TestMethod]
        [Ignore("Ignore this test due to incomplete code")]
        public void TestSendMessageInBatch()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            List<SendMessageBatchRequestEntry> batchMessage = new List<SendMessageBatchRequestEntry>();
            //batchMessage.AddRange("one");
            //batchMessage.AddRange("two");
            //batchMessage.Add("three");
            //batchMessage.Add("four");
            //batchMessage.Add("four");

            // Send message to the queue
            var request = new SendMessageBatchRequest(queueURL.Result.ToString(), batchMessage);

            // Get the response when sending the message request
            var response = sqsClient.SendMessageBatchAsync(request);

            // Verify the message body is correct between the request and the response
            ValidateMD5(request.Entries.ToString(), response.Result.ToString());
        }

        /// <summary>
        /// Send message in a queue with delay
        /// Steps:
        /// 1. Create the queue
        /// 2. Set the delay to 5 seconds
        /// 2. Send message to the queue
        /// 3. Verify the message body is correct between the request and the response
        /// </summary>
        [TestMethod]
        public void TestSendMessageWithDelay()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            // Set the request
            var request = new SendMessageRequest(queueURL.Result.ToString(), messageBody);

            // Set the delay to 5 seconds
            request.DelaySeconds = 5;

            // Get the response when sending the message request
            var response = sqsClient.SendMessageAsync(request);

            // Verify the message body is correct between the request and the response
            ValidateMD5(request.MessageBody, response.Result.MD5OfMessageBody);
        }

        /// <summary>
        /// Receive message 
        /// 1. Create the queue
        /// 2. Send the message
        /// 3. Receive the message that just being sent
        /// 4. Verify the message body is the same
        /// </summary>
        [TestMethod]
        public void TestReceiveMessage()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            // Send the message
            var request = new SendMessageRequest(queueURL.Result.ToString(), messageBody);
            sqsClient.SendMessageAsync(request);

            // Receive the message and verify that we have the message
            var receiveResponse = sqsClient.ReceiveMessageAsync(queueURL.Result.ToString());
            Assert.IsTrue(receiveResponse.Result.Messages.Count == 1, "Message is not created");

            // Verify the message body is same
            var messages = receiveResponse.Result.Messages;
            ValidateMD5(messages[0].Body, messages[0].MD5OfBody);
        }

        /// <summary>
        /// Delete Message
        /// Steps:
        /// 1. Create the queue
        /// 2. Send the message
        /// 3. Get message receipt handler
        /// 4. Delete the message
        /// 5. Verify that message is deleted from the queue
        /// </summary>
        [TestMethod]
        public async Task TestDeleteMessageAsync()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            // Send the message
            var request = new SendMessageRequest(queueURL.Result.ToString(), messageBody);
            await sqsClient.SendMessageAsync(request);

            // Receive the message
            var receiveResponse = await sqsClient.ReceiveMessageAsync(queueURL.Result.ToString());

            // Get the receipt handle for the message
            var messages = receiveResponse.Messages;
            messageReceipt = messages[0].ReceiptHandle;

            // Delete the message
            var deleteRequest = new DeleteMessageRequest(queueURL.Result.ToString(), messageReceipt);
            var response = sqsClient.DeleteMessageAsync(deleteRequest);
            Assert.IsTrue(response.Result.HttpStatusCode.ToString() == "OK", "Http Status Code is not OK");

            // Verify that there is no longer any message in the queue
            receiveResponse = await sqsClient.ReceiveMessageAsync(queueURL.Result.ToString());
            Assert.IsTrue(receiveResponse.Messages.Count == 0, "Message is not deleted");
        }

        /// <summary>
        /// Purge Queue
        /// Steps:
        /// 1. Create the queue
        /// 2. Send the message
        /// 3. Purge the queue
        /// 4. Verify the message is no longer in the queue
        /// </summary>
        [TestMethod]
        public async Task TestPurgeQueueAsync()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            // Send message
            var request = new SendMessageRequest(queueURL.Result.ToString(), messageBody);
            await sqsClient.SendMessageAsync(request);
            var receiveResponse = sqsClient.ReceiveMessageAsync(queueURL.Result.ToString());
            Assert.IsTrue(receiveResponse.Result.Messages.Count == 1, "Message is not created");

            // Purge the queue
            await sqsClient.PurgeQueueAsync(queueURL.Result.ToString());

            // Verify the message is no longer in the queue
            receiveResponse = sqsClient.ReceiveMessageAsync(queueURL.Result.ToString());
            Assert.IsTrue(receiveResponse.Result.Messages.Count == 0, "Message is not deleted");
        }

        /// <summary>
        /// Lists queue
        /// Steps:
        /// 1. Create 2 queue
        /// 2. Verify there are 2 queues in the list queue
        /// </summary>
        [TestMethod]
        public async Task TestListQueueAsync()
        {
            // Create queue no 1
            queueName = setQueueName();
            await sqsClient.CreateQueueAsync(queueName);

            // Create queue no 2
            queueName = setQueueName();
            await sqsClient.CreateQueueAsync(queueName);

            // Verify there are 2 queues in the list
            var sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 2, "Message count in the list queue is not the same");
        }
        #endregion

        #region Negative test
        /// <summary>
        /// Delete non existent queue
        /// Steps:
        /// 1. Do not create any queue
        /// 2. Get the list and make sure that the queue is created and make sure that we only create 1 queue
        /// 3. Get the list again, and verify that the list count is now 0
        /// </summary>
        [TestMethod]
        public void TestDeleteNonExistQueue()
        {
            // Do not create a queue
            queueName = setQueueName();

            // Get the list and make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = sqsClient.ListQueuesAsync(queueName);
            Assert.IsTrue(sqsQueueList.Result.QueueUrls.Count == 0, "There is something in the list");

            // Try to delete non existent queue
            try
            {
                sqsClient.DeleteQueueAsync(sqsQueueList.Result.QueueUrls[0].ToString());
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("Index was out of range. Must be non-negative and less than the size of the collection.\r\nParameter name: index", e.Message.ToString(), "Queue is able to be deleted, which is not suppose to");
            }

            // Get the list again, and verify that the list count is now 0
            sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.Result.QueueUrls.Count == 0, "The queue still have something in it");
        }

        /// <summary>
        /// Create same queue twice
        /// Steps:
        /// 1. Create the first queue
        /// 2. Create another queue that have the same name
        /// 3. Get the list again, and verify that the list count is still 1
        /// </summary>
        [TestMethod]
        public async Task TestCreateSameQueueTwiceAsync()
        {
            // Create the queue
            queueName = setQueueName();
            CreateQueueResponse createResponse = await sqsClient.CreateQueueAsync(queueName);

            // Get the queue list. Assert is added to make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.Result.QueueUrls.Count == 1, "Queue is not created or more than one queue is created");

            // Create another queue that have the same name
            createResponse = await sqsClient.CreateQueueAsync(queueName);

            // Verify there are only 1 queue created
            sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            Assert.IsFalse(sqsQueueList.Result.QueueUrls.Count == 2, "Extra queue was added in the list");
        }

        /// <summary>
        /// Send message in a queue with delay
        /// Steps:
        /// 1. Create the queue
        /// 2. Set the message to null
        /// 3. Send message to the queue
        /// 4. Verify the error message is correct
        /// </summary>
        [TestMethod]
        public void TestSendNullMessage()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            // Set the request
            var request = new SendMessageRequest(queueURL.Result.ToString(), null);

            // Get the response when sending the message request
            try
            {
                var response = sqsClient.SendMessageAsync(request);
            }
            catch (Exception e)
            {
                // Verify the error message is correct
                Assert.AreEqual("Error unmarshalling response back from AWS. Response Body: There was an internal server error.", e.Message.ToString());
            }
        }

        /// <summary>
        /// Send message in a queue with delay
        /// Steps:
        /// 1. Do not create the queue
        /// 2. Send the message to non existent queue
        /// 3. Verify the error message is correct
        /// </summary>
        [TestMethod]
        public void TestSendMessageToNoQueue()
        {
            // Do not create the queue
            queueName = setQueueName();

            // Send the message to non existent queue
            try
            {
                var request = new SendMessageRequest(queueURL.Result.ToString(), messageBody);
            }
            catch (Exception e)
            {
                // Verify the error message is correct
                Assert.AreEqual("Object reference not set to an instance of an object.", e.Message.ToString());
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Set the  queue name with prefix and randomize at the end
        /// so that we knows that every queue is unique
        /// </summary>
        /// <returns>String queue name</returns>
        private string setQueueName()
        {
            return prefix + new Random().Next();
        }

        /// <summary>
        /// Create the queue and return the queue URL
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns>String queue URL</returns>
        private async Task<string> createQueueURLAsync(string queueName)
        {
            // Create the queue
            await sqsClient.CreateQueueAsync(queueName);

            // List the queue
            var sqsQueueList = sqsClient.ListQueuesAsync(prefix);

            // Since we know that we only created 1 queue, we can say the array no 0
            return sqsQueueList.Result.QueueUrls[0].ToString();
        }

        /// <summary>
        /// Validate message body
        /// </summary>
        /// <param name="message"></param>
        /// <param name="md5"></param>
        private static void ValidateMD5(string message, string md5)
        {
            Amazon.SQS.Internal.ValidationResponseHandler.ValidateMD5(message, md5);
        }
        #endregion
    }
}