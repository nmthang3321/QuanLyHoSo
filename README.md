# QuanLyHoSo

Ung dung WPF .NET 5.0 quan ly ho so cho khu vuc An Giang.

File nay la huong dan bat buoc cho cac Copilot/AI agent hoac developer tiep theo khi lam viec tren repo.

## 1. Doc truoc khi code

Truoc khi sua code, hay doc theo thu tu:

1. `doc/proposal/Proposal_Phan_Mem_Quan_Ly_Ho_So_An_Giang_Updated.pdf`
2. GUI draft trong `doc/GUI/`
3. `doc/Thiet_Ke_He_Thong_Quan_Ly_Ho_So_WPF_NET5.md`
4. Source hien tai cua project WPF

Khong nen implement theo suy doan neu proposal hoac GUI da co thong tin ro rang.

## 2. Nguyen tac kien truc

Project dung WPF `.NET 5.0` va nen di theo huong:

```text
Views
  -> ViewModels
    -> Application / Use Cases
      -> Domain
        -> Infrastructure
```

Quy uoc:

- Uu tien MVVM.
- Khong dua nghiep vu vao code-behind XAML.
- View chi bind du lieu va command.
- ViewModel dieu phoi UI state, validation gan man hinh va goi use case.
- Domain chua entity, enum, rule nghiep vu.
- Infrastructure chua SQLite, file storage, export, backup, logging.
- Khong hard-code danh muc nghiep vu trong UI neu co the dua vao data/config.

## 3. Huong phat trien theo module

Man hinh chinh can follow GUI draft:

- Tong quan
- Nhap du lieu
- Phan loai & Xu ly
- Xuat du lieu
- Cai dat

Khi lam module nao, nen tao du:

- View
- ViewModel
- DTO/Model can thiet
- Use case/Application service
- Repository/service interface neu can truy cap data
- Log va error handling
- Test cho rule quan trong neu co

## 4. Logging va trace issue

He thong phai de trace khi co loi. Moi thao tac quan trong nen co `CorrelationId`.

Can log toi thieu:

- Module
- Action
- RecordCode neu thao tac lien quan ho so
- User/Processor neu co
- CorrelationId
- Exception stack trace khi loi

Khong log noi dung don/vu viec qua chi tiet neu co kha nang chua thong tin nhay cam.

Nen co audit cho:

- Tao/sua/xoa ho so
- Them/xoa file dinh kem
- Cap nhat trang thai xu ly
- Xuat du lieu
- Sao luu/khoi phuc
- Them/sua/xoa danh muc

## 5. Du lieu va file dinh kem

Huong thiet ke hien tai:

- Database local: SQLite.
- File dinh kem khong luu binary truc tiep vao DB.
- App copy file vao thu muc quan ly rieng.
- DB chi luu metadata va relative path.
- File hop le: PDF, JPG, PNG.
- Gioi han: 10 MB/file.

Khi xoa ho so hoac danh muc da duoc su dung, uu tien soft delete/`IsActive = false` de giu lich su.

## 6. Quy trinh xu ly ho so

Quy trinh nghiep vu:

```text
Tiep nhan -> Phan loai -> Phan cong -> Xac minh -> Gia han (neu co) -> Ket thuc -> Luu ho so
```

Moi lan cap nhat xu ly phai tao lich su gom:

- Trang thai cu
- Trang thai moi
- Buoc xu ly
- Ngay gio xu ly
- Nguoi xu ly
- Noi dung xu ly
- Ghi chu
- CorrelationId

Khong cho chuyen trang thai tuy tien; can co rule/policy kiem soat transition.

## 7. Git va account

Repo co the private. May phat trien co the dung nhieu account git.

Khi thao tac git:

- Khong sua `git config --global` neu khong duoc yeu cau ro.
- Uu tien config local repo neu can.
- Khong luu token vao remote URL.
- Khong commit file build/cache trong `bin/`, `obj/`.
- Khong revert thay doi cua nguoi khac neu khong duoc yeu cau.

## 8. Chat luong code

Khi them code:

- Dat ten class/method ro nghia.
- Tach ham khi logic dai hoac co rule nghiep vu rieng.
- Dung async cho IO/database/file/export.
- Bat va log exception o bien he thong; khong swallow exception im lang.
- Validate input o ViewModel va Application layer.
- Khong tao abstraction neu chua co ly do ro rang.
- Follow style san co cua repo.

## 9. Test va verify

Moi thay doi nen verify toi thieu:

- Build project.
- Chay app neu thay doi UI.
- Test manual luong nghiep vu lien quan.
- Them unit test cho business rule quan trong neu project test da co hoac khi them layer logic moi.

Nhung rule nen test:

- Sinh ma ho so.
- Chuyen trang thai ho so.
- Validate file dinh kem.
- Loc/tim kiem ho so.
- Export Excel/CSV.
- Backup/restore.

## 10. Tai lieu

Neu thay doi kien truc, data model, workflow hoac quy uoc logging, hay cap nhat:

- `doc/Thiet_Ke_He_Thong_Quan_Ly_Ho_So_WPF_NET5.md`
- `README.md`

README nay la ban do lam viec nhanh; file thiet ke trong `doc/` la tai lieu chi tiet hon.

