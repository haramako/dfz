require 'pp'
module Jekyll
  class TermDetector < Jekyll::Generator
    priority :high
    
    def generate(site)
      #ongoing, done = Book.all.partition(&:ongoing?)

      #reading = site.pages.detect {|page| page.name == 'reading.html'}
      site.pages.each do |page|
        page.data['title'] ||= page.name.gsub(/\.md$/,'')
        page.content.gsub(/\r/, '').scan /^#\s+(.+)/ do |mo|
          page.data['terms'] ||= []
          page.data['terms'] << mo[0].strip
        end
      end
      #reading.data['ongoing'] = ongoing
      #reading.data['done'] = done
    end
  end
end
