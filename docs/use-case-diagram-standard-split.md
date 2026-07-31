# UML Use Case - Split Diagrams

Quy uoc:

- Moi so do la 1 luong chinh rieng.
- Actor nam ben trai va chi noi den use case chinh.
- Use case chinh nam o giua.
- Use case phu nam ben phai, noi tu use case chinh bang `<<include>>` hoac `<<extend>>`.
- Tat ca duong noi ep huong ngang trai sang phai bang `-right-` va `-right->`.

## 1. User - Manage Account

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor User

rectangle "Online Auction System" {
  usecase "Manage Account" as UC_Main
  usecase "Register Account" as UC_Register
  usecase "Confirm Email" as UC_ConfirmEmail
  usecase "Login / Logout" as UC_LoginLogout
  usecase "Reset Password with OTP" as UC_ResetPassword
  usecase "Update Profile" as UC_UpdateProfile
  usecase "View Account Dashboard" as UC_Dashboard
}

User -right- UC_Main
UC_Main -right-> UC_Register : <<include>>
UC_Register -right-> UC_ConfirmEmail : <<include>>
UC_Main -right-> UC_LoginLogout : <<include>>
UC_LoginLogout -right-> UC_ResetPassword : <<extend>>
UC_Main -right-> UC_UpdateProfile : <<include>>
UC_Main -right-> UC_Dashboard : <<include>>

@enduml
```

## 2. User - Browse Marketplace

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor User

rectangle "Online Auction System" {
  usecase "Browse Marketplace" as UC_Main
  usecase "View Home Page" as UC_Home
  usecase "View Auction Listings" as UC_AuctionList
  usecase "View Buy Now Listings" as UC_BuyNowList
  usecase "Search / Filter Listings" as UC_SearchFilter
  usecase "View Listing Detail" as UC_Detail
  usecase "Download Product Document" as UC_Download
  usecase "View Seller Profile" as UC_SellerProfile
  usecase "Change Language" as UC_Language
}

User -right- UC_Main
UC_Main -right-> UC_Home : <<include>>
UC_Main -right-> UC_AuctionList : <<include>>
UC_Main -right-> UC_BuyNowList : <<include>>
UC_Main -right-> UC_SearchFilter : <<extend>>
UC_Main -right-> UC_Detail : <<extend>>
UC_Detail -right-> UC_Download : <<extend>>
UC_Detail -right-> UC_SellerProfile : <<extend>>
UC_Main -right-> UC_Language : <<extend>>

@enduml
```

## 3. User - Participate In Auction

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor User

rectangle "Online Auction System" {
  usecase "Participate In Auction" as UC_Main
  usecase "Register For Auction" as UC_Register
  usecase "Pay Registration Deposit" as UC_Deposit
  usecase "Cancel Registration" as UC_Cancel
  usecase "View Bid History / State" as UC_BidState
  usecase "Place Bid" as UC_PlaceBid
  usecase "Pass Bid Challenge" as UC_Challenge
  usecase "Add / Remove Watchlist" as UC_Watchlist
}

User -right- UC_Main
UC_Main -right-> UC_Register : <<include>>
UC_Register -right-> UC_Deposit : <<include>>
UC_Register -right-> UC_Cancel : <<extend>>
UC_Main -right-> UC_BidState : <<include>>
UC_Main -right-> UC_PlaceBid : <<include>>
UC_PlaceBid -right-> UC_Challenge : <<extend>>
UC_Main -right-> UC_Watchlist : <<extend>>

@enduml
```

## 4. User - Purchase Product

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor User

rectangle "Online Auction System" {
  usecase "Purchase Product" as UC_Main
  usecase "Add Buy Now Item To Cart" as UC_AddCart
  usecase "Complete Buy Now Order" as UC_BuyNowOrder
  usecase "Complete Won-Auction Order" as UC_WonAuctionOrder
  usecase "Checkout Payment" as UC_Checkout
  usecase "View Payment Confirmation" as UC_Confirm
  usecase "Handle Payment Return / Cancel" as UC_ReturnCancel
}

User -right- UC_Main
UC_Main -right-> UC_AddCart : <<include>>
UC_Main -right-> UC_BuyNowOrder : <<include>>
UC_Main -right-> UC_WonAuctionOrder : <<include>>
UC_BuyNowOrder -right-> UC_Checkout : <<include>>
UC_WonAuctionOrder -right-> UC_Checkout : <<include>>
UC_Checkout -right-> UC_Confirm : <<extend>>
UC_Checkout -right-> UC_ReturnCancel : <<extend>>

@enduml
```

## 5. User - Manage Own Listings

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor User

rectangle "Online Auction System" {
  usecase "Manage Own Listings" as UC_Main
  usecase "Create Auction Listing" as UC_CreateAuction
  usecase "Create Buy Now Listing" as UC_CreateBuyNow
  usecase "Edit Own Listing" as UC_Edit
  usecase "Delete Own Listing" as UC_Delete
  usecase "Track Verification Status" as UC_Track
}

User -right- UC_Main
UC_Main -right-> UC_CreateAuction : <<include>>
UC_Main -right-> UC_CreateBuyNow : <<include>>
UC_Main -right-> UC_Edit : <<include>>
UC_Main -right-> UC_Delete : <<include>>
UC_Main -right-> UC_Track : <<include>>

@enduml
```

## 6. User - Manage Personal Activity

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor User

rectangle "Online Auction System" {
  usecase "Manage Personal Activity" as UC_Main
  usecase "View My Bids" as UC_Bids
  usecase "View My Orders / Offers" as UC_Orders
  usecase "View My Watchlist" as UC_Watchlist
  usecase "View My Selling Listings" as UC_Selling
  usecase "View Submitted Listings" as UC_Submissions
  usecase "Manage Preferences" as UC_Preferences
}

User -right- UC_Main
UC_Main -right-> UC_Bids : <<include>>
UC_Main -right-> UC_Orders : <<include>>
UC_Main -right-> UC_Watchlist : <<include>>
UC_Main -right-> UC_Selling : <<include>>
UC_Main -right-> UC_Submissions : <<include>>
UC_Main -right-> UC_Preferences : <<extend>>

@enduml
```

## 7. User - Notifications And Complaints

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor User

rectangle "Online Auction System" {
  usecase "Notifications And Complaints" as UC_Main
  usecase "View Notifications" as UC_Notifications
  usecase "Register / Unregister Device Token" as UC_DeviceToken
  usecase "Mark Notification As Read" as UC_MarkRead
  usecase "Mark All Notifications As Read" as UC_MarkAllRead
  usecase "Submit Complaint / Refund Request" as UC_Submit
  usecase "View Complaint Confirmation" as UC_Confirmation
}

User -right- UC_Main
UC_Main -right-> UC_Notifications : <<include>>
UC_Main -right-> UC_DeviceToken : <<include>>
UC_Notifications -right-> UC_MarkRead : <<extend>>
UC_Notifications -right-> UC_MarkAllRead : <<extend>>
UC_Main -right-> UC_Submit : <<include>>
UC_Submit -right-> UC_Confirmation : <<extend>>

@enduml
```

## 8. Admin - Authentication

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Admin Authentication" as UC_Main
  usecase "Admin Login" as UC_Login
  usecase "Admin Logout" as UC_Logout
  usecase "View Access Denied" as UC_AccessDenied
}

Admin -right- UC_Main
UC_Main -right-> UC_Login : <<include>>
UC_Main -right-> UC_Logout : <<include>>
UC_Login -right-> UC_AccessDenied : <<extend>>

@enduml
```

## 9. Admin - Dashboard And Reports

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Dashboard And Reports" as UC_Main
  usecase "View Overview Metrics" as UC_Overview
  usecase "View Auction Statistics" as UC_AuctionStats
  usecase "View Revenue Statistics" as UC_RevenueStats
  usecase "Export Report" as UC_Export
}

Admin -right- UC_Main
UC_Main -right-> UC_Overview : <<include>>
UC_Main -right-> UC_AuctionStats : <<include>>
UC_Main -right-> UC_RevenueStats : <<include>>
UC_Main -right-> UC_Export : <<extend>>

@enduml
```

## 10. Admin - Manage Users And Permissions

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Manage Users And Permissions" as UC_Main
  usecase "View User List" as UC_List
  usecase "Create User" as UC_Create
  usecase "Edit User" as UC_Edit
  usecase "View User Detail" as UC_Detail
  usecase "Delete / Bulk Action User" as UC_Delete
  usecase "Assign Permissions" as UC_Assign
}

Admin -right- UC_Main
UC_Main -right-> UC_List : <<include>>
UC_Main -right-> UC_Create : <<include>>
UC_Main -right-> UC_Edit : <<include>>
UC_Main -right-> UC_Detail : <<include>>
UC_Main -right-> UC_Delete : <<include>>
UC_Main -right-> UC_Assign : <<include>>

@enduml
```

## 11. Admin - Manage Catalog

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Manage Catalog" as UC_Main
  usecase "Manage Categories" as UC_Category
  usecase "Manage Product Templates" as UC_Template
  usecase "Manage Products" as UC_Product
  usecase "Download Product Documents" as UC_Download
}

Admin -right- UC_Main
UC_Main -right-> UC_Category : <<include>>
UC_Main -right-> UC_Template : <<include>>
UC_Main -right-> UC_Product : <<include>>
UC_Main -right-> UC_Download : <<include>>

@enduml
```

## 12. Admin - Manage Auctions

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Manage Auctions" as UC_Main
  usecase "View Auction List" as UC_List
  usecase "View Auction Detail" as UC_Detail
  usecase "View DB Status And Listing Phase" as UC_Phase
  usecase "Create / Edit Auction" as UC_CreateEdit
  usecase "Cancel Auction" as UC_Cancel
  usecase "Delete / Bulk Delete Auction" as UC_Delete
  usecase "Review Fraud Alert" as UC_Fraud
  usecase "Dismiss Fraud Alert" as UC_DismissFraud
}

Admin -right- UC_Main
UC_Main -right-> UC_List : <<include>>
UC_Main -right-> UC_Detail : <<include>>
UC_Main -right-> UC_Phase : <<include>>
UC_Main -right-> UC_CreateEdit : <<include>>
UC_Main -right-> UC_Cancel : <<include>>
UC_Main -right-> UC_Delete : <<include>>
UC_Detail -right-> UC_Fraud : <<extend>>
UC_Fraud -right-> UC_DismissFraud : <<extend>>

@enduml
```

## 13. Admin - Verify Listings

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Verify Listings" as UC_Main
  usecase "View Pending Submissions" as UC_Pending
  usecase "View Submission Detail" as UC_Detail
  usecase "Approve Listing" as UC_Approve
  usecase "Reject Listing" as UC_Reject
  usecase "Notify User" as UC_Notify
}

Admin -right- UC_Main
UC_Main -right-> UC_Pending : <<include>>
UC_Main -right-> UC_Detail : <<include>>
UC_Main -right-> UC_Approve : <<include>>
UC_Main -right-> UC_Reject : <<include>>
UC_Approve -right-> UC_Notify : <<include>>
UC_Reject -right-> UC_Notify : <<include>>

@enduml
```

## 14. Admin - Manage Buy Now

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Manage Buy Now" as UC_Main
  usecase "View Buy Now List" as UC_List
  usecase "View Buy Now Detail" as UC_Detail
  usecase "Create / Edit Buy Now Listing" as UC_CreateEdit
  usecase "Cancel Buy Now Listing" as UC_Cancel
  usecase "Bulk Delete Buy Now Listing" as UC_BulkDelete
}

Admin -right- UC_Main
UC_Main -right-> UC_List : <<include>>
UC_Main -right-> UC_Detail : <<include>>
UC_Main -right-> UC_CreateEdit : <<include>>
UC_Main -right-> UC_Cancel : <<include>>
UC_Main -right-> UC_BulkDelete : <<include>>

@enduml
```

## 15. Admin - Handle Complaints And Refunds

```plantuml
@startuml
left to right direction
skinparam linetype ortho
skinparam shadowing false
skinparam nodesep 35
skinparam ranksep 45

actor Admin

rectangle "Online Auction System" {
  usecase "Handle Complaints And Refunds" as UC_Main
  usecase "View Complaint List" as UC_List
  usecase "View Complaint Detail" as UC_Detail
  usecase "Update Complaint Status" as UC_Update
  usecase "Process Refund Decision" as UC_Refund
  usecase "Notify User About Result" as UC_Notify
}

Admin -right- UC_Main
UC_Main -right-> UC_List : <<include>>
UC_Main -right-> UC_Detail : <<include>>
UC_Main -right-> UC_Update : <<include>>
UC_Main -right-> UC_Refund : <<include>>
UC_Update -right-> UC_Notify : <<include>>
UC_Refund -right-> UC_Notify : <<include>>

@enduml
```

