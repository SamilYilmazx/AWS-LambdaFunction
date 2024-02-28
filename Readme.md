# AWS Lambda Simple S3 Function Project

This project consists of:
* Function.cs - class file containing a class with a single function handler method
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS

The generated function handler responds to events on an Amazon S3 bucket. The handler receives the bucket and object key details in an S3Event instance and returns the content type of the object as the function output.

This project is simply for extracting your .zip files and write them into the bucket. I developed this for my .html contents which are compressed in a zip file. You can change the code to suits your needs.

After deploying your function you must configure an Amazon S3 bucket as an event source to trigger your Lambda function.

## Deploy function to AWS Lambda:

Open command line and run the code below. When the zip file created, you can deploy it by choosing your code from .zip option on AWS Lambda Function menu.

```
    cd "AWSLambda/src/AWSLambda"
    dotnet lambda package --package-type:zip
```
