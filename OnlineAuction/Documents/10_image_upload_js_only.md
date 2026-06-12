### Xử lý ảnh, upload và lưu trữ với cloudinary.
- Cloudinary
    - Dịch vụ lưu trữ file với dung lượng free lến 500Mb.
    - Hỗ trợ việc upload file cho nhiều ngôn ngữ khác nhau, trong đó có cả widget bằng html, css, js (dùng được ở nhiều các web framework khác nhau).
    - Hỗ trợ chỉnh sửa ảnh, crop, thay đổi kích thước. Điều này dẫn đến chúng ta có thể lấy ảnh phù hợp
      cho các thiết bị khác nhau: máy tính thì ảnh size to, mobile thì ảnh size nhỏ.
- Lý do.
    - Việc sử dụng html, js, css widget có thể dùng đi dùng lại trên các project với các ngôn ngữ khác.
    - Phù hợp trong quá trình học, thử nghiệm với 500Mb miễn phí.
    - Đơn giản, hiệu quả, tránh được các vấn đề liên quan đến bảo mật khi upload ảnh trực tiếp lên server chứa code php.
- Chuẩn bị.
    - Tài khoản cloudinary.
    - Tạo upload preset.
- Cách nhúng widget vào trang.
    - Copy paste đoạn code sau.

          <button id="upload_widget" class="cloudinary-button">Upload files</button>
          <script src="https://upload-widget.cloudinary.com/global/all.js" type="text/javascript"></script>
          <script type="text/javascript">  
          var myWidget = cloudinary.createUploadWidget({
            cloudName: 'my_cloud_name', 
            uploadPreset: 'my_preset'}, function (error, result) { 
              if (!error && result && result.event === "success") { 
                //console.log('Done! Here is the image info: ', result.info.url); 
                console.log('Done! Here is the image info: ', result.info.secure_url); 
              }
            }
          )
          
          document.getElementById("upload_widget").addEventListener("click", function(){
              myWidget.open();
            }, false);
          </script>
    - Nút bấm upload có thể sửa theo style của template hiện tại. Nên bổ sung một trường hidden
      để khi upload thành công sẽ lấy link ảnh đã upload lưu vào trường hidden field. Trường hidden này
      sẽ được gửi lên kèm form và vẫn có thể validate bình thường.
    - Có thể tạo các thẻ image preview để giúp người dùng có thể nhìn được những ảnh vừa upload.
    - Trường hợp lưu nhiều ảnh thì các ảnh có thể lưu thành chuỗi cách nhau bởi dấu ",". Việc này liên quan đến vấn đề
      lấy ảnh và hiển thị ảnh tại các trang khác. Giải quyết như sau.
        - Thêm thuộc tính cho model xử lý bóc tách ảnh để trả về một mảng các url.

              public function getListPhotoAttribute()
              {
                  $array_image = [];
                  if ($this->thumbnail) {
                  $array_image = explode(',', $this->thumbnail);
                  }
                  //do whatever you want to do
                  return $array_image;
              }
        - Thêm thuộc tính để model có thể dễ dàng lấy ra được ảnh default cũng như trong trường hợp mà
          không có ảnh (hiển thị ảnh default.)

              public function getDefaultThumbnailAttribute()
              {
                  $array_image = $this->listPhoto;
                      if (count($array_image) > 0) {
                      return $array_image[0];
                  } else {
                  return 'https://link-anh-default.jpg';
                  }
              }
        - Khi hiển thị ảnh ngoài view thì có thể dùng đoạn code sau.

             <img src="{{$obj->defaultThumbnail}}" style="width: 300px" alt="">

        hoặc hiển thị danh sách ảnh như sau.

            @foreach($obj->listPhoto as $url)
                <img src="{{$url}}" style="width: 300px" alt="">
            @endforeach