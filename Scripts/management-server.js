


function showMessage(type, title, message){
  BootstrapDialog.show({
    type:    type,
    title:   title,
    message: message,
    buttons: [{
               label:    Localization.DLG_BUTTON_CLOSE,
               cssClass: "btn-default btn-sm",
               action:   function(dialog) { dialog.close(); }
             }]
  });
}



function errorMessage(title,message){
  showMessage(BootstrapDialog.TYPE_WARNING, title, message);
}



function setMBeanProperty(e){
  var input = $(e.currentTarget).parent().parent().find("input");
  $.ajax({
    url:      "/set-prop",
    dataType: "json",
    data:     {
                id:       $("#mbeanview .panel-body input[name*='object-id']").val(),
                propName: input.attr("name"),
                val:      input.val()
              },
    type:     "POST",
    success: function(d){
      showMessage(
        (d.result === "ok")? BootstrapDialog.TYPE_SUCCESS: BootstrapDialog.TYPE_WARNING,
        (d.result === "ok")? Localization.DLG_CAPTION_SUCCESS: Localization.DLG_CAPTION_ERROR,
        (d.message)
      );
    },
    error: function() {
      errorMessage(Localization.DLG_CAPTION_ERROR, Localization.AJAX_ERROR);
    }
  });
}



function showMBeanDetails(d){
  $("#mbeanview > .panel > .panel-heading > .panel-title").text(d.mbeanName);

  var mbeanPropPanel = $("#mbean-properties")
    .empty();

  if (d.description){
    mbeanPropPanel
      .append("<h5></h5>")
      .find("h5")
      .text(d.description);
  }

  if (d.properties){
    var mbeanPropTable = mbeanPropPanel
      .append("<fieldset><table class='table table-bordered table-condensed'></table></fieldset>")
      .find("table");
    mbeanPropTable
      .append("<tr style='display:none;'><td colspan='3'><input type='hidden' name='object-id'/></td></tr>")
      .find("input[name*='object-id']")
      .val(d.id);

    for (var i = 0; i < d.properties.length; i++){
      var p = d.properties[i];
      mbeanPropTable.append(
        "<tr><td><label for='" + p.name + "'></label></td><td><input type='text' "
          + "name='" + p.name + "' id='" + p.name
          + "' " + (p.writable? "": "readonly") + "/></td>"
          + "<td>" + (p.writable? "<button type='button' class='btn btn-default btn-sm' onclick='setMBeanProperty(event);'>"
          + "<span class='glyphicon glyphicon-ok-sign' aria-hidden='true'></span></button>" : "") + "</td></tr>")
        .find("label[for*='" + p.name + "']")
        .text((p.label)? p.label: p.name);

      var mbeanPropInput = mbeanPropTable
        .find("input[name*='" + p.name + "']");

      if (p.description){
        mbeanPropInput.tooltip({
          trigger: "hover",
          title:   p.description
        });
      }
      if ("value" in p){
        mbeanPropInput.val(p.value);
      } else {
        mbeanPropInput.attr("placeholder", Localization.STR_PROP_WRITEONLY);
      }
    }
  }



  var methodsPanel = $("#mbean-methods").empty();
  if (d.methods){
    for (var i = 0; i < d.methods.length; i++){
      var m = d.methods[i];
      var div = methodsPanel
        .append("<div class='panel panel-default'></div>")
        .find("div").last();           
      div
        .append(
          "<div class='panel-heading'>"
          + "<h4 class='panel-title'>"
            + "<a data-toggle='collapse' data-parent='#mbean-methods' href='#collapseMethod" + i + "'></a>"
          + "</h4>"
        + "</div>"
        + "<div id='collapseMethod" + i + "' class='panel-collapse collapse'>"
          + "<div class='panel-body'>"
            + "<p></p>"
          + "</div>"
        + "</div>"
        )
        .find("a")
        .text("(" + m.returnType + ") " + m.name);
      if (m.description){
        div.find("a").tooltip({
          trigger: "hover",
          title:   m.description
        });
      }

      var form = $(div).find("p").parent()
        .append("<form class='form-horizontal' role='form' action='/invoke' method='POST'></form>")
        .find("form");
      form
        .append("<input type='hidden' name='object-id' value='" + d.id + "'></input>")
        .append("<input type='hidden' name='method-name' value='" + m.name + "'></input>")
        .append("<input type='hidden' name='method-signature' value='" + m.signature + "'></input>");

      if (m.parameters){
        for (var j = 0; j < m.parameters.length; j++){
          var p = m.parameters[j];
          var formGroup = $(form)
            .append("<div class='form-group'></div>")
            .find("div").last();
          formGroup
            .append("<label class='control-label col-sm-2' for='" + p.name + "'></label>")
            .find("label")
            .text(p.label? p.label: p.name);
          var input = $(formGroup)
            .append("<div class='col-sm-10'></div>")
            .find("div")
            .append("<input type='text' class='form-control' name='" + p.name + "'></input>")
            .find("input");
          input.attr("placeholder", p.type);
          if (p.description){
            input.tooltip({
              trigger: "hover",
              title:   p.description
            });
          }
        }
      }

      form
        .append("<div class='form-group'></div>")
        .find("div").last()
          .append("<div class='col-sm-offset-2 col-sm-10'></div>")
          .find("div")
            .append("<button type='submit' class='btn btn-default'></button>")
            .find("button")
            .text(Localization.BTN_INVOKE);

      form
        .removeAttr("onsubmit")
        .submit(function(event){
          var $form = $(this);
          $.ajax({
            url:      $form.attr("action"),
            dataType: "json",
            data:     $form.serialize(),
            type:     $form.attr("method"),
            success: function(d){
              showMessage(
                (d.result === "ok")? BootstrapDialog.TYPE_SUCCESS: BootstrapDialog.TYPE_WARNING,
                (d.result === "ok")? Localization.DLG_CAPTION_SUCCESS: Localization.DLG_CAPTION_ERROR,
                (d.message)
              );
            },
            error: function(){
              errorMessage(Localization.DLG_CAPTION_ERROR, Localization.AJAX_ERROR);
            }
          });
    
          event.preventDefault();
        });
    }
  }
}




$(function(){


  // Подставить локализованные строки в страницу

  $(".localized").each(function(){
    $(this).text(
      eval($(this).attr("text-resource-id"))
    );
  });


  // Загрузить дерево объектов управления

  $.ajax({
    url:      "/tree",
    dataType: "json",
    success:  function(d) {
      $( "#mbean-hierarchy" ).treeview({
        data:       d,
        showBorder: false,
        onNodeSelected: function(e,n){
          if (n.tags.length > 0 && n.tags[0] == "mbean"){
            $.ajax({
              url:      "/mbean",
              dataType: "json",
              data:     { id: n.id },
              type:     "GET",
              success:  function(data){
                showMBeanDetails(data);
              },
              error: function(){
                errorMessage(Localization.DLG_CAPTION_ERROR, Localization.AJAX_ERROR_MBEAN);
              }
            });
          } else {
            $("#mbeanview .panel-title").text(Localization.STR_MBEAN_SELECT);
            $("#mbean-properties").empty();
            $("#mbean-methods").empty();
          }
        }
      });      
    },
    error: function() {
      errorMessage(Localization.DLG_CAPTION_ERROR, Localization.AJAX_ERROR);
    }
  });


});
