# Product Template Management

Admin Product Management now uses a two-level model:

1. Mau san pham (`product_templates`)
2. San pham cua seller (`products`)

`categories` remain broad groups such as Pokemon or One Piece. A mau san pham is the canonical card identity, for example `Charizard Base Set 4/102 PSA 10`. A product is one seller-owned instance of that mau, with its own seller, cert number, price, condition, image override, and auction links.

## Data Model

`product_templates` stores shared card metadata:

- `name`
- `category_id`
- `set_name`
- `card_number`
- `grade_label`
- `year`
- `language`
- `short_description`
- `description_html`
- `primary_image`
- `is_active`
- audit fields

`products.product_template_id` links each seller product to its mau san pham.

For phase 1, `products` still keeps snapshot fields such as `name`, `category_id`, `set_name`, `card_number`, `grade_label`, `year`, `language`, and descriptions. This keeps existing public product, auction, order, and marketplace flows stable while Admin moves to the new model.

## Admin Flow

`/Admin/Product` lists mau san pham.

Each row shows:

- preview image
- ten mau
- category
- set and card number
- grade
- seller product count
- last product added

`/Admin/Product/Template/{id}` lists only products whose `product_template_id` matches the selected mau. This page is used to compare sellers, cert numbers, and prices for the same card identity.

## Product Creation

When Admin creates a product from a mau san pham, the form locks the selected mau and copies template metadata into product snapshot fields. Admin still enters seller-specific data:

- seller
- cert number
- estimated value
- import price
- condition
- grading sub-scores
- product image override
- documents

If no product image is uploaded, the product uses the mau san pham primary image.

## Migration

The migration creates `product_templates`, adds `products.product_template_id`, then backfills existing products by grouping on:

```text
category_id
UPPER(TRIM(name))
UPPER(TRIM(set_name))
UPPER(TRIM(card_number))
UPPER(TRIM(grade_label))
```

The first product in each group provides the initial mau san pham metadata. Existing active products are then linked to the matching mau.
