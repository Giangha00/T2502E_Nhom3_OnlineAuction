# Contact Page - Testing Guide

## 🧪 Quick Start

### Run the Application
```powershell
cd "C:\Users\Nguyễn Hữu Quân\Downloads\T2502E_Nhom3_OnlineAuction\OnlineAuction"
dotnet run
```

**Application URL:** `http://localhost:5006`
**Contact Page:** `http://localhost:5006/Contact`

---

## 📋 Test Cases

### 1. Page Layout & Structure

**Test 1.1: Hero Section Display**
- Navigate to `/Contact`
- Verify gradient background (amber to stone)
- Check title: "Contact Auction House" visible
- Check subtitle: "Have questions? We're here to help" visible
- ✅ PASS: All elements visible and properly positioned

**Test 1.2: Contact Information Cards**
- Scroll down to Contact Information section
- Verify 4 cards displayed in grid layout:
  - Address card with icon
  - Phone card with icon
  - Email card with icon  
  - Working Hours card with icon
- Verify card styling (rounded corners, shadows, hover effects)
- ✅ PASS: All cards visible with correct information

**Test 1.3: Contact Form Layout**
- Verify form section displays with title "Send us a Message"
- Verify form is on the right side (desktop) or below (mobile)
- Check all form fields are visible:
  - Full Name input
  - Email input
  - Subject input
  - Message textarea
  - Send Message button
- ✅ PASS: All form elements properly arranged

**Test 1.4: Location Section**
- Scroll to "Our Location" section
- Verify map placeholder displays
- Verify location details card shows:
  - Main Office heading
  - Address with icon
  - Phone with icon
  - Business hours table
  - Get Directions button
- ✅ PASS: Location section complete

**Test 1.5: Footer**
- Scroll to bottom
- Verify footer contains:
  - Auction House logo
  - Navigation sections (About, Contact, Help, Legal)
  - Copyright year
- ✅ PASS: Footer properly rendered

---

### 2. Responsive Design

**Test 2.1: Mobile (< 576px)**
- Open DevTools (F12)
- Set viewport to iPhone SE (375px width)
- Verify:
  - Hero section displays full width
  - Contact info cards stack vertically (1 column)
  - Form layout is single column
  - All text readable without horizontal scroll
- ✅ PASS: Mobile layout correct

**Test 2.2: Tablet (768px - 1024px)**
- Set viewport to iPad (768px width)
- Verify:
  - Contact info cards show 2 per row
  - Form is single column or 2-column depending on screen
  - No content overflow
- ✅ PASS: Tablet layout correct

**Test 2.3: Desktop (> 1024px)**
- Set viewport to 1920x1080
- Verify:
  - Contact info cards show 4 in one row
  - Form content (left + right) displayed side-by-side
  - Max-width container (max-w-7xl) applied
- ✅ PASS: Desktop layout correct

---

### 3. Form Validation

**Test 3.1: Full Name Validation**
1. Focus on Full Name field
2. Leave it empty and click elsewhere (blur)
   - ✅ Error message appears: "This field is required"
   - ✅ Input border turns red
3. Type a name
   - ✅ Error clears
   - ✅ Border returns to normal

**Test 3.2: Email Validation**
1. Focus on Email field
2. Test with empty value
   - ✅ Shows "This field is required"
3. Test with invalid format (e.g., "testgmail.com")
   - ✅ Shows "Invalid email format"
4. Test with valid email (e.g., "test@gmail.com")
   - ✅ No error message

**Test 3.3: Message Validation**
1. Focus on Message field
2. Leave empty and blur
   - ✅ Shows "This field is required"
3. Type a message
   - ✅ Error clears

**Test 3.4: Subject Field (Optional)**
1. Leave Subject field empty
2. Try to submit form
   - ✅ Form submits without error (optional field)

**Test 3.5: Form Submission**
1. Fill in ALL required fields correctly:
   - Full Name: "John Doe"
   - Email: "john@example.com"
   - Message: "Test message"
2. Click "Send Message" button
   - ✅ Button text changes to "✓ Message Sent Successfully!"
   - ✅ Button background turns green
   - ✅ Form clears
3. Wait 3 seconds
   - ✅ Button reverts to original state

**Test 3.6: Submit with Missing Fields**
1. Leave Full Name empty
2. Fill Email: "test@test.com"
3. Fill Message: "Test"
4. Click Submit
   - ✅ Error shows under Full Name: "This field is required"
   - ✅ Form does NOT submit

---

### 4. Interactive Elements

**Test 4.1: Hover Effects (Desktop)**
1. Hover over contact info cards
   - ✅ Shadow increases
   - ✅ Border changes color to amber
2. Hover over form button
   - ✅ Background color changes (hover:bg-amber-800)
3. Hover over phone/email links
   - ✅ Color changes to hover state

**Test 4.2: Focus States (Keyboard Navigation)**
1. Tab through form fields
2. Each field should show:
   - ✅ Focus ring (amber color)
   - ✅ Border highlight
3. Tab to submit button
   - ✅ Button shows focus state
4. Press Enter
   - ✅ Form validates and submits if valid

**Test 4.3: Links**
1. Click phone number: "+84 123 456 789"
   - ✅ Initiates phone call (tel:// protocol)
2. Click email links
   - ✅ Opens email client (mailto:// protocol)
3. Click "Get Directions" button
   - ✅ Link functions (navigates or shows map)

---

### 5. Visual Design

**Test 5.1: Typography**
- Headings are bold and appropriately sized
- Body text is readable (contrast ratio > 4.5:1)
- Labels are clear and visible
- Error messages are visible in red

**Test 5.2: Color Consistency**
- Primary color: amber-700 used throughout
- Secondary: stone tones for backgrounds
- Errors: red-600 for error states
- Consistent with Home/About pages

**Test 5.3: Spacing & Alignment**
- Content properly padded with px-4/px-6/px-8
- Vertical spacing consistent (py-12/py-16)
- Grid gaps consistent (gap-6/gap-8)
- Max-width container applied (max-w-7xl)

**Test 5.4: Icons**
- All icon SVGs render correctly
- Icon colors match design system
- Icons have proper sizing and alignment

---

### 6. Navigation

**Test 6.1: Header Links**
1. From any page, click "Contact Us" in navbar
   - ✅ Navigates to `/Contact`
2. Mobile menu works
   - ✅ Click hamburger menu
   - ✅ "Contact Us" option visible and clickable

**Test 6.2: Internal Navigation**
1. From Contact page, click "Home" link
   - ✅ Navigates to Home page
2. Click "About Us" link
   - ✅ Navigates to About page

**Test 6.3: Footer Links**
1. Click footer links
   - ✅ Contact Us navigates to Contact page
   - ✅ About Us navigates to About page
   - ✅ Other links function as expected

---

### 7. Browser Compatibility

**Test with Multiple Browsers:**

✅ Chrome/Edge Latest
```
- Open: http://localhost:5006/Contact
- All CSS renders correctly
- Form validation works
- Responsive design functions
```

✅ Firefox Latest
```
- Same testing as above
- Check email input type="email" validation
```

✅ Safari Latest
```
- Same testing as above
```

---

### 8. Performance

**Test 8.1: Page Load**
- Open DevTools Network tab
- Go to `/Contact`
- Verify:
  - Page loads in < 1 second
  - CSS file loads (output.css)
  - JavaScript file loads (contact.js)
  - No 404 errors

**Test 8.2: Form Response**
- Fill form and submit
- JavaScript validation response is instant (< 100ms)
- No lag or delays

---

### 9. Accessibility

**Test 9.1: Keyboard Navigation**
1. Press Tab repeatedly through entire page
   - ✅ Can reach all form fields
   - ✅ Submit button reachable
   - ✅ All links reachable

**Test 9.2: Screen Reader (Optional)**
- Use built-in screen reader
- Test labels are properly announced
- Required fields marked with *
- Error messages are announced

**Test 9.3: Color Contrast**
- Use browser DevTools accessibility checker
- All text has sufficient contrast
- Error messages in red are visible

---

### 10. Form Data Validation

**Test 10.1: Valid Inputs**
```
Full Name: "John Doe"
Email: "john@example.com"
Subject: "Question about bidding"
Message: "I would like to know more..."
Result: ✅ Form accepts and validates
```

**Test 10.2: Invalid Email Formats**
```
"test"           → ❌ Invalid
"test@"          → ❌ Invalid
"test@domain"    → ❌ Invalid (no TLD)
"test@domain.c"  → ✅ Valid (technically)
```

**Test 10.3: Special Characters in Name**
```
"John O'Brien"   → ✅ Valid
"José García"    → ✅ Valid
"李明文"          → ✅ Valid
```

---

## 📊 Test Summary Template

```
Contact Page Testing Report
============================

Date: [DATE]
Tester: [NAME]

✓ Layout & Structure:     [PASS/FAIL]
✓ Responsive Design:      [PASS/FAIL]
✓ Form Validation:        [PASS/FAIL]
✓ Interactive Elements:   [PASS/FAIL]
✓ Visual Design:          [PASS/FAIL]
✓ Navigation:             [PASS/FAIL]
✓ Browser Compatibility:  [PASS/FAIL]
✓ Performance:            [PASS/FAIL]
✓ Accessibility:          [PASS/FAIL]
✓ Data Validation:        [PASS/FAIL]

Overall: ✅ PASS / ❌ FAIL

Issues Found:
- [Issue 1]
- [Issue 2]

Notes:
[Any additional notes]
```

---

## 🐛 Known Issues / Limitations

1. **Map Placeholder**: Google Maps is not integrated (placeholder only)
2. **Email Not Sent**: Form submission is client-side only
3. **No Data Persistence**: No database backend integration
4. **No Ticket System**: Support tickets not implemented

---

## ✅ Acceptance Criteria Verification

| Criteria | Status | Evidence |
|----------|--------|----------|
| Hero Section | ✅ PASS | Visual inspection in DevTools |
| Contact Info | ✅ PASS | 4 cards with icons visible |
| Contact Form | ✅ PASS | All fields render correctly |
| Form Validation | ✅ PASS | JavaScript validation works |
| Location Section | ✅ PASS | Map placeholder & details visible |
| Footer | ✅ PASS | Footer loads with all links |
| Responsive | ✅ PASS | Tested on 3 breakpoints |
| Component Separation | ✅ PASS | 5 partial views created |
| No Major UI Issues | ✅ PASS | No visual glitches observed |

---

## 📞 Support

For issues or questions:
1. Check the implementation summary: `CONTACT_PAGE_IMPLEMENTATION.md`
2. Review the code comments in partial views
3. Check browser console for JavaScript errors (F12)
4. Verify all files are in correct locations

**Project Location:**
`C:\Users\Nguyễn Hữu Quân\Downloads\T2502E_Nhom3_OnlineAuction`

