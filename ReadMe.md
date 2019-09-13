# LambdaSharp - Create Animated GIFs from Videos with AWS Lambda

[This sample requires the LambdaSharp CLI to deploy.](https://lambdasharp.net/)

## Overview

This LambdaSharp module creates a Lambda function that converts videos to animated GIFs. The conversion is done by the [FFmpeg application](https://www.ffmpeg.org/) which is deployed as a [Lambda Layer](https://docs.aws.amazon.com/lambda/latest/dg/configuration-layers.html). The module uses two S3 buckets: one for uploading videos and one for storing the converted animated GIFs. The Lambda function is automatically invoked when a file is uploaded the video S3 bucket.

## Deploy

This module is compiled to CloudFormation and deployed using the LambdaSharp CLI.

```
git clone https://github.com/LambdaSharp/GifMaker-Sample.git
cd GifMaker-Sample
lash deploy
```

## Details

1. Compress contents of `Assets/FFmpeg-Layer` folder into a zip package with  execution permissions for `ffmpeg` executable.
1. Create a Lambda layer using the uploaded package.
1. Create Lambda function
1. (Optional) Create an S3 bucket for uploading videos.
1. (Optional) Create an S3 bucker for storing the converted animated GIFs.


## Parameters

<dl>

<dt><code>VideoBucket</code></dt>
<dd>
The <code>VideoBucket</code> parameter can optionally be provided to specify the ARN of an existing S3 bucket where video will be uploaded. A new S3 bucket is automatically created when this parameter is omitted.

<i>Required</i>: No

<i>Type</i>: String (S3 Bucket ARN)
</dd>

<dt><code>AnimatedGifBucket</code></dt>
<dd>
The <code>AnimatedGifBucket</code> parameter can optionally be provided to specify the ARN of an existing S3 bucket where the converted animated GIF files are uploaded to. A new S3 bucket is automatically created when this parameter is omitted.

<i>Required</i>: No

<i>Type</i>: String (S3 Bucket ARN)
</dd>

</dl>


## Attribution

The FFmepg application is provided by https://www.ffmpeg.org/


## License

_Apache 2.0_ for the module and code.

_GPL 3.0_ for the files in the `Assets/FFmpeg-Layer` folder.