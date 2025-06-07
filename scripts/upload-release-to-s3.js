require("dotenv").config();

const child = require("child_process");
const fs = require("fs");
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

async function uploadString(name, str) {
    const uploadResult = await s3Client.putObject({
        ACL: "public-read",
        Bucket: BUCKET_NAME,
        Key: name,
        Body: Buffer.from(str, "utf-8"),
        ContentType: "plain/text",
    });
    console.log("string upload result:", uploadResult);
}

async function uploadFile(path, destPath, contentType) {
    const fileStream = fs.createReadStream(path);
    const uploadResult = await s3Client.putObject({
        ACL: "public-read",
        Bucket: BUCKET_NAME,
        Key: destPath,
        Body: fileStream,
        ContentType: contentType,
    });
    console.log("file upload result:", uploadResult);
}

async function main() {
    console.log("fetching latest release tag for CoffeeUpdateClient...");
    const latestTag = child.execSync("gh release list -L 1 --json tagName -q \".[].tagName\"").toString().replace(/^\s+|\s+$/g, "");
    console.log("latest release tag for CoffeeUpdateClient:", latestTag);

    console.log("downloading latest releases...");
    child.execSync(`gh release download ${latestTag} -D release --clobber -p "*.zip"`, { stdio: "inherit" });

    const clientName = `CoffeeUpdateClient-${latestTag}.zip`;
    const clientPath = `release/${clientName}`;

    await uploadFile(clientPath, `client/${clientName}`, "application/vnd.microsoft.portable-executable");

    const clientUrl = `https://${BUCKET_NAME}.${BUCKET_ENDPOINT}/client/${clientName}`;
    const manifest = `<?xml version="1.0" encoding="UTF-8"?>
<item>
  <version>${latestTag}</version>
  <url>${clientUrl}</url>
  <mandatory mode="2">true</mandatory>
</item>`;
    console.log("updating client.xml with latest versions:", manifest);
    await uploadString("client.xml", manifest);
    console.log("Client URL:", clientUrl);
}

main();