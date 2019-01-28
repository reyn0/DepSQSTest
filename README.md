PREREQUISITE:

Make sure you have JRE and JDK installed in your machine.

Download the elasticmq standalone server from https://s3-eu-west-1.amazonaws.com/softwaremill-public/elasticmq-server-0.14.6.jar

Before we run this, make sure to start elasticmq server by running this at the command prompt:
java -jar elasticmq-server-0.14.6.jar

Then go to http://localhost:9324/ in the web browser to make sure the server is run.
Once you finished with the test, you can turn off the command prompt by Ctrl+C.

Pull the code from:
https://github.com/reyn0/DepSQSTest

==================================================================================

HOW TO RUN:

The test can be run from Visual Studio
- Open the Deputy.sln with Visual Studio
- Build Solution from Visual Studio. I use VS2017 with .Net Core 2.1
- Run the tests from test explorer in Visual Studio

==================================================================================

Technology:
I use C#

Why:
I needed to use language that I am familiar with and something that can be easily built and tested

Technical details:
- First I set up my environment properly so that I can run the stand-alone elasticmq
- Added into github now instead of sharing the files directly
- I started with a simple create queue test as that is the most important in AWS. Without the queue, we cannot manipulate the messages.
- I then started working on Delete Queue, Get Queue URL, Send Message, Retrieve Message and Delete Message.
- Since the last time, the AWS SDK is updated and all of my tests and function is no longer working. 
  They have removed most of the non-Async method, so I need to update to use the async function.
  All the test should be working.
- Based on the last feedback, I have addressed, improved and added some of the following:
  - Split the test to be more specific and make it 2 areas, queue and messages, instead of 1 big SQSTest
  - In the TestCreateQueue, I have a check to make sure there is no queue created. Also added verification for the queue-specific name.
  - Added test for sending a message with delay
  - Added test for creating a queue which already exists
  - Added region in all code for easy reading.
  - Added negative tests such as:
    - Added test for deleting a queue when none exists
    - Added test for sending a message to none existent queue

What can be tested further:

If I had more time to implement this, I would like to expand the test further about:
- Batch sending messages and batch delete messages. (code is started, but having issue with the batching)
- Some extended functionality like Change Message Visibility, Get Queue Attributes, Dead Letter Queue, Permissions, etc.
- End to end test that covers most of the functionality. (code is added, but having issue with the sending messages)
- Reduce some repetitive in my code. 
- Create a base class for test initiation, clean up and some of the commonly shared methods
