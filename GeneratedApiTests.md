# API Test Cases — PAN Verification API  
**Endpoint:** `POST /risk/pan/verify`  
**Authorization:** Function Key  
**Content-Type:** application/json  

---

# 1. Positive Test Cases

### TC-PAN-001 Valid PAN Verification
**Description:** Verify PAN with valid input data.

**Request**
```json
{
  "userId": "user123",
  "panNumber": "ABCDE1234F",
  "name": "Rahul Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": true,
  "requestSource": "mobile_app"
}
```

**Expected Result**
- PAN format validated
- Provider verification called
- Request stored in repository
- Result stored in repository

**Expected HTTP Status**
`200 OK`

**Expected Response**
- `status = VERIFIED`
- `requestId` generated
- `panNumber = ABCDE1234F`
- `nameMatchScore >= 0`
- `panStatus = ACTIVE`
- `category = INDIVIDUAL`
- `verifiedAt` populated

---

### TC-PAN-002 Valid PAN with Mixed Case Input
**Description:** PAN should be normalized to uppercase.

**Request**
```json
{
  "userId": "user123",
  "panNumber": "abcde1234f",
  "name": "Rahul Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected Result**
- PAN converted to uppercase
- Verification succeeds

**Expected HTTP Status**
`200 OK`

---

### TC-PAN-003 Valid PAN with Leading/Trailing Spaces
**Request**
```json
{
  "userId": "user123",
  "panNumber": "  ABCDE1234F  ",
  "name": "Rahul Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected Result**
- PAN trimmed
- Verification succeeds

**Expected HTTP Status**
`200 OK`

---

### TC-PAN-004 Name Slightly Different (Name Match Score)
**Description:** Verify fuzzy name match scoring.

**Request**
```json
{
  "userId": "user123",
  "panNumber": "ABCDE1234F",
  "name": "Rahul S Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected Result**
- NameMatchScore calculated using Levenshtein
- Score between `0` and `1`

**Expected HTTP Status**
`200 OK`

---

# 2. Negative Test Cases

### TC-PAN-005 Missing Consent
**Request**
```json
{
  "userId": "user123",
  "panNumber": "ABCDE1234F",
  "name": "Rahul Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": false
}
```

**Expected Result**
- Verification rejected

**Expected HTTP Status**
`400 Bad Request`

**Expected Response**
```json
{
  "status": "FAILED",
  "reason": "CONSENT_REQUIRED"
}
```

---

### TC-PAN-006 Null Request Body
**Request**
```
null
```

**Expected Result**
- Request validation fails

**Expected HTTP Status**
`400 Bad Request`

**Expected Response**
```
reason = CONSENT_REQUIRED
```

---

### TC-PAN-007 Invalid PAN Format (Too Short)
**Request**
```json
{
  "panNumber": "ABC123",
  "name": "Rahul Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected HTTP Status**
`400 Bad Request`

**Expected Response**
```
reason = INVALID_PAN
```

---

### TC-PAN-008 Invalid PAN Format (Wrong Pattern)
**Request**
```json
{
  "panNumber": "12345ABCDE",
  "name": "Rahul Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected HTTP Status**
`400 Bad Request`

**Expected Response**
```
reason = INVALID_PAN
```

---

### TC-PAN-009 PAN With Special Characters
**Request**
```json
{
  "panNumber": "ABCDE12@4F",
  "name": "Rahul Sharma",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected HTTP Status**
`400 Bad Request`

---

# 3. Edge Cases

### TC-PAN-010 Name Empty
**Request**
```json
{
  "panNumber": "ABCDE1234F",
  "name": "",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected Result**
- NameMatchScore = 0

**Expected HTTP Status**
`200 OK`

---

### TC-PAN-011 Name Null
**Request**
```json
{
  "panNumber": "ABCDE1234F",
  "name": null,
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected Result**
- NameMatchScore = 0

**Expected HTTP Status**
`200 OK`

---

### TC-PAN-012 Extremely Long Name
**Request**
```json
{
  "panNumber": "ABCDE1234F",
  "name": "RahulRahulRahulRahulRahulRahulRahulRahulRahulRahul",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected Result**
- API processes successfully
- NameMatchScore computed

**Expected HTTP Status**
`200 OK`

---

### TC-PAN-013 Future Date of Birth
**Request**
```json
{
  "panNumber": "ABCDE1234F",
  "name": "Rahul Sharma",
  "dateOfBirth": "2050-01-01",
  "consent": true
}
```

**Expected Result**
- No DOB validation exists
- Provider called

**Expected HTTP Status**
`200 OK`

---

# 4. Provider Failure Cases

### TC-PAN-014 Provider Returns Failure
**Precondition**
Provider returns:
```
Success = false
```

**Expected Result**
- API returns failure response

**Expected HTTP Status**
`500 Internal Server Error`

**Expected Response**
```
status = FAILED
reason = PROVIDER_ERROR
requestId generated
```

---

# 5. Data Integrity Cases

### TC-PAN-015 Request ID Format Validation
**Expected Result**
- RequestId generated in format:
```
panreq_{32_char_guid}
```

Example:
```
panreq_8d6cfe9c3f3c4b0f8a12a8a0d0d12345
```

**Expected HTTP Status**
`200 OK`

---

### TC-PAN-016 PAN Hash Storage
**Validation**
- SHA256 hash stored in repository
- Last 4 digits stored separately

**Expected**
- No plaintext PAN stored in DB logs or repository layer.

---

# 6. Security Test Cases

### TC-PAN-017 SQL Injection in Name Field
**Request**
```json
{
  "panNumber": "ABCDE1234F",
  "name": "'; DROP TABLE USERS;--",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected Result**
- No SQL execution
- Treated as string
- Verification succeeds

**Expected HTTP Status**
`200 OK`

---

### TC-PAN-018 Script Injection in Name
**Request**
```json
{
  "panNumber": "ABCDE1234F",
  "name": "<script>alert('x')</script>",
  "dateOfBirth": "1990-05-10",
  "consent": true
}
```

**Expected HTTP Status**
`200 OK`

---

# 7. Performance Test Cases

### TC-PAN-019 High Request Volume
**Scenario**
- 500 requests per minute

**Expected Result**
- No timeout
- Each request generates unique `requestId`

**Expected HTTP Status**
`200 OK`

---

# 8. Contract Validation

### TC-PAN-020 Response Schema Validation

**Expected Response Fields**

- `requestId` (string)
- `panNumber` (string)
- `status` (string)
- `nameMatchScore` (double)
- `panStatus` (string)
- `category` (string)
- `verifiedAt` (datetime)
- `statusCode` (int)

**Expected HTTP Status**
`200 OK`