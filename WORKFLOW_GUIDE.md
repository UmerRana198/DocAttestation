# MOFA Approval Workflow Guide

## Overview

The Document Attestation system uses a **3-Level Sequential Approval Workflow** for processing applications. Each application must pass through all three levels before final approval.

## Workflow Levels

### Level 1: Verification Officer
- **Role**: `VerificationOfficer`
- **Status**: `UnderVerification`
- **Responsibilities**:
  - Review the submitted document
  - Verify document authenticity
  - Check document details against application information
  - Approve to move to next level OR Reject/Send Back

### Level 2: Supervisor
- **Role**: `Supervisor`
- **Status**: `UnderSupervision`
- **Responsibilities**:
  - Secondary review of verified documents
  - Quality assurance check
  - Approve to move to final level OR Reject/Send Back

### Level 3: Attestation Officer
- **Role**: `AttestationOfficer`
- **Status**: `UnderAttestation`
- **Responsibilities**:
  - Final review and approval
  - Official attestation
  - Once approved, document is marked as `Approved` and ready for download

## Application Status Flow

```
Draft → Submitted → UnderVerification → UnderSupervision → UnderAttestation → Approved
                                                              ↓
                                                          Rejected/SentBack
```

## How the Workflow Works

### 1. Application Submission
When an applicant submits an application:
- Status changes from `Draft` to `Submitted`
- QR code is generated and embedded in the PDF
- Document is stamped with QR code
- **Note**: Workflow assignment must be done manually by Admin or automatically configured

### 2. Workflow Assignment
An Admin or system must assign the application to a VerificationOfficer:
- Navigate to Admin panel
- Assign application to a VerificationOfficer
- Application status changes to `UnderVerification`

### 3. Level 1: Verification Officer Review
1. VerificationOfficer logs in
2. Goes to "Pending Applications" (Workflow/Index)
3. Sees applications assigned to them
4. Clicks "Review" to view application details
5. Can view original and stamped documents
6. Takes action:
   - **Approve**: Moves to Level 2 (Supervision)
   - **Reject**: Application is rejected (requires remarks)
   - **Send Back**: Returns to applicant for corrections (requires remarks)

### 4. Level 2: Supervisor Review
1. Supervisor logs in
2. Sees applications that passed Level 1
3. Reviews the application
4. Takes action:
   - **Approve**: Moves to Level 3 (Attestation)
   - **Reject**: Application is rejected
   - **Send Back**: Returns to applicant or previous level

### 5. Level 3: Attestation Officer Review
1. AttestationOfficer logs in
2. Sees applications that passed Level 2
3. Performs final review
4. Takes action:
   - **Approve**: Application is **APPROVED** ✅
     - Status changes to `Approved`
     - `AttestedAt` timestamp is set
     - Document is ready for download
   - **Reject**: Application is rejected
   - **Send Back**: Returns to applicant or previous level

### 6. Applicant Download
Once approved:
- Applicant can view application status in "My Applications"
- Download button appears for approved applications
- Downloads the final attested document with QR code

## Actions Available at Each Level

### Approve
- Moves application to next level (or completes if at Level 3)
- Remarks are optional
- Creates audit trail entry
- Updates application status

### Reject
- Immediately rejects the application
- **Remarks are REQUIRED**
- Status changes to `Rejected`
- Application cannot proceed further
- Creates audit trail entry

### Send Back
- Returns application for corrections
- **Remarks are REQUIRED**
- Status changes to `SentBack`
- Application may need to restart workflow
- Creates audit trail entry

## Document Viewing

### For Applicants
- **View Original Document**: View the original PDF uploaded
- **View Stamped Document**: View PDF with QR code (if available)
- **Download Attested Document**: Download final approved document (only when approved)

### For Officers
- **View Original Document**: Review original uploaded document
- **View Stamped Document**: Review document with QR code stamp
- All documents open in new browser tab for review

## Workflow Assignment

Currently, workflow assignment must be done manually. To assign an application to a VerificationOfficer:

1. Admin logs in
2. Navigate to applications list
3. Select application
4. Assign to VerificationOfficer
5. Application status changes to `UnderVerification`

**Future Enhancement**: Automatic assignment based on workload balancing or routing rules.

## Audit Trail

Every action in the workflow is logged:
- Who performed the action
- When it was performed
- What action was taken
- Previous and new status
- Remarks/notes
- IP address (for security)

## Roles and Permissions

- **Applicant**: Can submit applications, view status, download approved documents
- **VerificationOfficer**: Can review and approve/reject applications at Level 1
- **Supervisor**: Can review and approve/reject applications at Level 2
- **AttestationOfficer**: Can review and approve/reject applications at Level 3
- **Admin**: Full access, can assign workflows, manage users

## Important Notes

1. **Sequential Processing**: Applications must complete each level before moving to the next
2. **Remarks Required**: Reject and Send Back actions require remarks
3. **Document Security**: All documents are hashed for tamper detection
4. **QR Code Verification**: Each document has a unique QR code for verification
5. **Audit Compliance**: Complete audit trail for all actions

## Troubleshooting

### Application stuck at "Submitted"
- Check if workflow has been assigned to a VerificationOfficer
- Admin needs to assign the application manually

### Cannot view documents
- Ensure file paths are correct
- Check file permissions
- Verify documents were uploaded successfully

### Workflow not progressing
- Verify user has correct role
- Check if previous level was approved
- Review audit trail for errors

