require("dotenv").config();

const { S3 } = require("@aws-sdk/client-s3");

const {
    BUCKET_ENDPOINT,
    BUCKET_REGION,
    BUCKET_NAME,
    BUCKET_ACCESS_KEY_ID,
    BUCKET_SECRET_ACCESS_KEY,
    S3_PREFIX,
} = process.env;

const s3Client = new S3({
    forcePathStyle: false,
    endpoint: `https://${BUCKET_ENDPOINT}`,
    region: BUCKET_REGION,
    credentials: {
        accessKeyId: BUCKET_ACCESS_KEY_ID,
        secretAccessKey: BUCKET_SECRET_ACCESS_KEY,
    }
});

async function main() {
    const prefix = S3_PREFIX || "releases/";
    console.log(`Listing objects with prefix: ${prefix}`);

    const { Contents } = await s3Client.listObjectsV2({
        Bucket: BUCKET_NAME,
        Prefix: prefix,
    });

    if (!Contents || Contents.length === 0) {
        console.log("No objects found");
        return;
    }

    for (const obj of Contents) {
        console.log(`Setting public-read ACL on ${obj.Key}`);
        await s3Client.putObjectAcl({
            Bucket: BUCKET_NAME,
            Key: obj.Key,
            ACL: "public-read",
        });
    }

    console.log(`Set public-read ACL on ${Contents.length} object(s)`);
}

main();
