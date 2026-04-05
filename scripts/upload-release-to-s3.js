require("dotenv").config();

const child = require("child_process");
const fs = require("fs");
const path = require("path");
const { S3 } = require("@aws-sdk/client-s3");

const {
    BUCKET_ENDPOINT,
    BUCKET_REGION,
    BUCKET_NAME,
    BUCKET_ACCESS_KEY_ID,
    BUCKET_SECRET_ACCESS_KEY,
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

async function uploadFile(filePath, destPath, contentType) {
    const fileStream = fs.createReadStream(filePath);
    const uploadResult = await s3Client.putObject({
        ACL: "public-read",
        Bucket: BUCKET_NAME,
        Key: destPath,
        Body: fileStream,
        ContentType: contentType,
    });
    console.log(`uploaded ${destPath}:`, uploadResult);
}

function getContentType(fileName) {
    if (fileName.endsWith(".nupkg")) return "application/octet-stream";
    if (fileName.endsWith(".exe")) return "application/vnd.microsoft.portable-executable";
    return "application/octet-stream";
}

async function main() {
    const latestTag = process.env.RELEASE_TAG;
    if (!latestTag) {
        throw new Error("RELEASE_TAG environment variable is required");
    }
    console.log("release tag:", latestTag);

    console.log("downloading latest release assets...");
    child.execSync(`gh release download ${latestTag} -D release --clobber`, { stdio: "inherit" });

    // Upload all Velopack release artifacts to the releases/ prefix in S3
    const releaseDir = "release";
    const files = fs.readdirSync(releaseDir);
    console.log("release assets:", files);

    for (const file of files) {
        const filePath = path.join(releaseDir, file);
        const destPath = `releases/${file}`;
        const contentType = getContentType(file);
        console.log(`uploading ${file} -> ${destPath}`);
        await uploadFile(filePath, destPath, contentType);
    }

    console.log("all release artifacts uploaded successfully");
}

main();
