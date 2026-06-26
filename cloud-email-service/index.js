import express from "express";
import { google } from "googleapis";
import "dotenv/config";
const app = express();

app.use(express.json());

const {
    API_KEY,
    GMAIL_CLIENT_ID,
    GMAIL_CLIENT_SECRET,
    GMAIL_REFRESH_TOKEN,
    SENDER_EMAIL
} = process.env;

function makeEmail({ to, fullName, confirmUrl, locale }) {
    const subject =
        locale === "vi-VN"
            ? "Kích hoạt tài khoản OnlineAuction"
            : "Activate your OnlineAuction account";

    const html = `
    <div style="font-family: Arial, sans-serif; line-height: 1.6;">
      <h2>RareCard Auction House</h2>

      <p>Xin chào ${fullName || "bạn"},</p>

      <p>Tài khoản của bạn đang ở trạng thái chờ kích hoạt.</p>

      <p>Vui lòng bấm nút bên dưới để hoàn tất đăng ký:</p>

      <p>
        <a href="${confirmUrl}"
           style="display:inline-block;padding:12px 18px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:6px;">
          Kích hoạt tài khoản
        </a>
      </p>

      <p>Nếu nút không hoạt động, hãy copy link sau:</p>

      <p style="word-break: break-all;">${confirmUrl}</p>

      <p>Link có hiệu lực trong 24 giờ.</p>
    </div>
  `;

    return [
        `From: RareCard Auction House <${SENDER_EMAIL}>`,
        `To: ${to}`,
        `Subject: ${subject}`,
        "MIME-Version: 1.0",
        "Content-Type: text/html; charset=UTF-8",
        "",
        html
    ].join("\n");
}

function base64UrlEncode(message) {
    return Buffer.from(message)
        .toString("base64")
        .replace(/\+/g, "-")
        .replace(/\//g, "_")
        .replace(/=+$/, "");
}

app.get("/health", (req, res) => {
    res.json({ status: "ok" });
});

app.post("/send-verification", async (req, res) => {
    try {
        const requestApiKey = req.header("X-API-KEY");

        if (!API_KEY || requestApiKey !== API_KEY) {
            return res.status(401).json({
                success: false,
                message: "Unauthorized"
            });
        }

        const { to, fullName, confirmUrl, locale } = req.body;

        if (!to || !confirmUrl) {
            return res.status(400).json({
                success: false,
                message: "Missing required fields"
            });
        }

        const oauth2Client = new google.auth.OAuth2(
            GMAIL_CLIENT_ID,
            GMAIL_CLIENT_SECRET
        );

        oauth2Client.setCredentials({
            refresh_token: GMAIL_REFRESH_TOKEN
        });

        const gmail = google.gmail({
            version: "v1",
            auth: oauth2Client
        });

        const rawMessage = base64UrlEncode(
            makeEmail({ to, fullName, confirmUrl, locale })
        );

        const result = await gmail.users.messages.send({
            userId: "me",
            requestBody: {
                raw: rawMessage
            }
        });

        return res.json({
            success: true,
            messageId: result.data.id
        });
    } catch (error) {
        console.error("Send email error:", error.message);

        return res.status(500).json({
            success: false,
            message: "Send email failed"
        });
    }
});

const port = process.env.PORT || 8080;

app.listen(port, () => {
    console.log(`Email service running on port ${port}`);
});