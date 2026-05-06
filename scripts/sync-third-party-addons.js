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

const ADDONS = [
    { name: "BigWigs", repo: "BigWigsMods/BigWigs" },
    { name: "M33kAuras", repo: "m33shoq/M33kAuras" },
    { name: "NorthernSkyRaidTools", repo: "Reloe/NorthernSkyRaidTools" },
    { name: "RCLootCouncil", repo: "evil-morfar/RCLootCouncil2" },
];

async function getString(key) {
    try {
        const data = await s3Client.getObject({
            Bucket: BUCKET_NAME,
            Key: key,
        });
        return data.Body.transformToString();
    } catch (e) {
        console.error("Error getting string", key, e);
        return false;
    }
}

async function uploadString(key, str) {
    const uploadResult = await s3Client.putObject({
        ACL: "public-read",
        Bucket: BUCKET_NAME,
        Key: key,
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

function getLatestReleaseTag(repo) {
    return child
        .execSync(`gh release list -R ${repo} -L 1 --json tagName -q ".[].tagName"`)
        .toString()
        .replace(/^\s+|\s+$/g, "");
}

function downloadReleaseAsset(repo, tag, assetPattern, destDir) {
    fs.mkdirSync(destDir, { recursive: true });
    child.execSync(
        `gh release download ${tag} -R ${repo} -D ${destDir} --clobber -p "${assetPattern}"`,
        { stdio: "inherit" }
    );
}

async function main() {
    const currentManifestString = await getString("manifest.json");
    if (!currentManifestString) {
        throw new Error("Failed to fetch current manifest from S3");
    }
    console.log("current manifest:", currentManifestString);
    const manifest = JSON.parse(currentManifestString);

    const updates = [];

    for (const addon of ADDONS) {
        console.log(`\n--- ${addon.name} ---`);

        const latestTag = getLatestReleaseTag(addon.repo);
        console.log(`latest release: ${latestTag}`);

        const existing = manifest.AddOns.find(a => a.Name === addon.name);
        const currentVersion = existing?.Version;
        console.log(`manifest version: ${currentVersion}`);

        if (currentVersion === latestTag) {
            console.log(`${addon.name} is already up to date, skipping`);
            continue;
        }

        const assetName = `${addon.name}-${latestTag}.zip`;
        const localDir = "release";
        const localPath = `${localDir}/${assetName}`;

        console.log(`downloading ${assetName}...`);
        downloadReleaseAsset(addon.repo, latestTag, assetName, localDir);

        const remotePath = `addons/${assetName}`;
        console.log(`uploading to S3: ${remotePath}`);
        await uploadFile(localPath, remotePath, "application/zip");

        if (existing) {
            existing.Version = latestTag;
        } else {
            manifest.AddOns.push({ Name: addon.name, Version: latestTag });
        }

        updates.push({ name: addon.name, oldVersion: currentVersion ?? "(none)", newVersion: latestTag });
        console.log(`${addon.name} updated: ${currentVersion} -> ${latestTag}`);
    }

    if (updates.length === 0) {
        console.log("\nAll addons are up to date, nothing to do");
        return;
    }

    const newManifestString = JSON.stringify(manifest, null, 2);
    console.log("\nnew manifest:", newManifestString);
    await uploadString("manifest.json", newManifestString);

    console.log(`\nDone. Updated ${updates.length} addon(s).`);
}

main();
